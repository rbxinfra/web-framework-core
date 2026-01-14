namespace Roblox.Web.Framework.Services.Http;

using System;
using System.Net;
using System.Linq;
using System.Collections.Concurrent;

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;

using Prometheus;

using EventLog;
using Instrumentation;
using Framework.Common;
using ApplicationContext;

using Api.ControlPlane;
using Operations.Monitoring;
using Roblox.Service.ApiControlPlane;

/// <summary>
/// An <see cref="ActionFilterAttribute"/> that validates an Api Key
/// </summary>
public class ValidateApiKeyAttribute : ActionFilterAttribute
{
    private readonly IApplicationContext _ApplicationContext;
    private readonly IApiKeyParser _ApiKeyParser;
    private readonly IAuthority _Authority;
    private readonly ILogger _Logger;
    private readonly ICounterRegistry _CounterRegistry;
    private readonly IServiceSettings _Settings;

    private const string _ApiClientHttpContextKey = "ApiClient";
    private const string _UnknownHttpMetricsApplicationName = "Unknown";
    private const string _HttpMetricsApplicationNameHeader = "Roblox-Application-Name";
    private const string _UnknownClientMessage = "API Key was passed but isn't associated with a known client";

    private static readonly Type _AnonymousAttributeType = typeof(AllowAnonymousAttribute);
    private static readonly ConcurrentDictionary<string, PerOperationApiKeyPerformanceMonitor> _PerOperationApiKeyPerformanceMonitors = new();

    /// <summary>
    /// The API key in the request was missing, or unauthorized.
    /// </summary>
    private static readonly Counter _UnauthorizedApiKeyCounter = Metrics.CreateCounter(
        "unauthorized_api_keys",
        "The API key in the request was missing, or unauthorized.",
        "operation_name"
    );

    /// <summary>
    /// An API key was included in the request, and it is authorized for the operation it is executing for.
    /// </summary>
    private static readonly Counter _AuthorizedApiKeyCounter = Metrics.CreateCounter(
        "authorized_api_keys",
        "An API key was included in the request, and it is authorized for the operation it is executing for.",
        "operation_name",
        "application_name",
        "client_name"
    );

    /// <summary>
    /// Create a new instance of <see cref="ValidateApiKeyAttribute"/>
    /// </summary>
    /// <param name="counterRegistry">The <see cref="ICounterRegistry"/></param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="applicationContext">The <see cref="IApplicationContext"/>.</param>
    /// <param name="apiKeyParser">The <see cref="IApiKeyParser"/></param>
    /// <param name="authority">The <see cref="IAuthority"/></param>
    /// <param name="settings">The <see cref="IServiceSettings"/></param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="counterRegistry"/> cannot be null.
    /// - <paramref name="logger"/> cannot be null.
    /// - <paramref name="applicationContext"/> cannot be null.
    /// - <paramref name="apiKeyParser"/> cannot be null.
    /// - <paramref name="authority"/> cannot be null.
    /// - <paramref name="settings"/> cannot be null.
    /// </exception>
    public ValidateApiKeyAttribute(
        ICounterRegistry counterRegistry,
        ILogger logger,
        IApplicationContext applicationContext,
        IApiKeyParser apiKeyParser,
        IAuthority authority,
        IServiceSettings settings
    )
    {
        _CounterRegistry = counterRegistry ?? throw new ArgumentNullException(nameof(counterRegistry));
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ApplicationContext = applicationContext ?? throw new ArgumentNullException(nameof(applicationContext));
        _ApiKeyParser = apiKeyParser ?? throw new ArgumentNullException(nameof(apiKeyParser));
        _Authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc cref="ActionFilterAttribute.OnActionExecuting(ActionExecutingContext)"/>
    public override void OnActionExecuting(
        ActionExecutingContext context
    )
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        Validate(context);
    }

    private static void ReturnStatus(ActionExecutingContext context, HttpStatusCode statusCode, string reasonPhrase)
    {
        context.Result = new HttpStatusCodeResult((int)statusCode, reasonPhrase);
    }

    private void Validate(ActionExecutingContext actionContext)
    {
        if (!ShouldValidateApiKey(actionContext))
        {
            // Action does not require authorization.
            return;
        }

        var actionName = actionContext.RouteData?.Values["action"] as string ?? string.Empty;

        if (string.IsNullOrEmpty(actionName)) return;

        var serviceName = _ApplicationContext.Name;

        /* The ServiceIsEnabled and OperationIsEnabled operations on ServiceAuthority always return true as IsAuthorized will validate status. */

        if (!_Authority.ServiceIsEnabled(serviceName))
        {
            ReturnStatus(actionContext, HttpStatusCode.ServiceUnavailable, $"Service ({serviceName}) is disabled.");
            return;
        }

        if (!_Authority.OperationIsEnabled(serviceName, actionName))
        {
            ReturnStatus(actionContext, HttpStatusCode.ServiceUnavailable, $"Operation ({actionName}) is disabled (on service: {serviceName})");
            return;
        }

        var performanceMonitor = _PerOperationApiKeyPerformanceMonitors.GetOrAdd(actionName, new PerOperationApiKeyPerformanceMonitor(_CounterRegistry, actionName));

        var (validated, statusMessage) = TryValidateApiKey(serviceName, actionName, actionContext, performanceMonitor, out var client);
        if (validated) return;
        
        ReturnStatus(actionContext, HttpStatusCode.Unauthorized, statusMessage);
    }

    private static bool ShouldValidateApiKey(ActionExecutingContext actionContext)
    {
        if (actionContext.ActionDescriptor is not ControllerActionDescriptor controllerActionDescriptor) return true;
        
        var allowAnonymousAttributes = controllerActionDescriptor.MethodInfo.GetCustomAttributes(
            attributeType: _AnonymousAttributeType,
            inherit: true).ToList();

        allowAnonymousAttributes.AddRange(controllerActionDescriptor.ControllerTypeInfo.GetCustomAttributes(
            attributeType: _AnonymousAttributeType,
            inherit: true));

        return allowAnonymousAttributes.Count == 0;

    }


    private (bool, string) TryValidateApiKey(string serviceName, string operationName, ActionExecutingContext actionContext, PerOperationApiKeyPerformanceMonitor performanceMonitor, out IApiClient client)
    {
        client = default(IApiClient);

        if (
            _ApiKeyParser.TryParseApiKey(
                actionContext.HttpContext.Request,
                out var apiKey
            )
        )
        {
            try
            {
                if (!_Authority.IsAuthorized(apiKey, serviceName, operationName, out client))
                {
                    _UnauthorizedApiKeyCounter.WithLabels(operationName).Inc();
                    performanceMonitor.UnauthorizedApiKeys.Increment();

                    return (false, $"Client ({client?.Note ?? _UnknownClientMessage}) is not authorized for {operationName} (on service: {serviceName})");
                }

                _AuthorizedApiKeyCounter.WithLabels(operationName, GetApplicationName(actionContext), client.Note).Inc();
                performanceMonitor.AuthorizedApiKeys.Increment();

                actionContext.HttpContext.Items.Add(_ApiClientHttpContextKey, client);

                return (true, default);
            }
            catch (Exception ex)
            {
                _Logger.Error(ex);

                _UnauthorizedApiKeyCounter.WithLabels(operationName).Inc();
                performanceMonitor.UnauthorizedApiKeys.Increment();

                return _Settings.VerboseErrorsEnabled ? 
                    throw new ApplicationException("An error occurred while validating the API key, check inner exception.", ex) 
                    : (false, $"Client ({_UnknownClientMessage}) is not authorized for {operationName} (on service: {serviceName})");
            }
        }

        _UnauthorizedApiKeyCounter.WithLabels(operationName).Inc();
        performanceMonitor.UnauthorizedApiKeys.Increment();

        return (false, $"API key ({ApiKeyParser.ApiKeyHeaderName}) not specified in request to {serviceName} ({operationName})");
    }

    private static string GetApplicationName(ActionExecutingContext actionExecutingContext)
    {
        if (!actionExecutingContext.HttpContext.Request.Headers.TryGetValue(_HttpMetricsApplicationNameHeader, out var headers))
            return _UnknownHttpMetricsApplicationName;

        return headers.FirstOrDefault() ?? _UnknownHttpMetricsApplicationName;
    }
}
