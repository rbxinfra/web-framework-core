namespace Roblox.Web.Framework.Services.Http;

using System;
using System.Net;
using System.Linq;
using System.Collections.Concurrent;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;

using Prometheus;

using EventLog;
using Instrumentation;
using ApplicationContext;

using Operations;
using Api.ControlPlane;
using Operations.Monitoring;

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

    private static void ReturnStatus(ActionExecutingContext context, HttpStatusCode statusCode, OperationError operationError)
    {
        context.Result = new JsonResult(operationError) { StatusCode = (int)statusCode };
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
            ReturnStatus(actionContext, HttpStatusCode.ServiceUnavailable, new(ApiControlPlaneErrors.ServiceDisabled, serviceName));
            return;
        }

        if (!_Authority.OperationIsEnabled(serviceName, actionName))
        {
            ReturnStatus(actionContext, HttpStatusCode.ServiceUnavailable, new(ApiControlPlaneErrors.OperationDisabled, actionName, serviceName));
            return;
        }

        var performanceMonitor = _PerOperationApiKeyPerformanceMonitors.GetOrAdd(actionName, new PerOperationApiKeyPerformanceMonitor(_CounterRegistry, actionName));

        var (validated, statusMessage) = TryValidateApiKey(serviceName, actionName, actionContext, performanceMonitor);
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


    private (bool, OperationError) TryValidateApiKey(string serviceName, string operationName, ActionExecutingContext actionContext, PerOperationApiKeyPerformanceMonitor performanceMonitor)
    {
        if (
            _ApiKeyParser.TryParseApiKey(
                actionContext.HttpContext.Request,
                out var apiKey
            )
        )
        {
            try
            {
                if (!_Authority.IsAuthorized(apiKey, serviceName, operationName, out var client))
                {
                    _UnauthorizedApiKeyCounter.WithLabels(operationName).Inc();
                    performanceMonitor.UnauthorizedApiKeys.Increment();

                    if (client == null)
                        return (false, new OperationError(ApiControlPlaneErrors.ApiKeyUnauthorizedForService, serviceName, operationName));

                    return (false, new OperationError(ApiControlPlaneErrors.ApiKeyUnauthorizedForOperation, client.Note, operationName, serviceName));
                }

                _AuthorizedApiKeyCounter.WithLabels(operationName, actionContext.HttpContext.GetRequestingApplicationName(), client.Note).Inc();
                performanceMonitor.AuthorizedApiKeys.Increment();

                actionContext.HttpContext.SetCurrentApiClient(client);

                return (true, default);
            }
            catch (Exception ex)
            {
                _Logger.Error(ex);

                _UnauthorizedApiKeyCounter.WithLabels(operationName).Inc();
                performanceMonitor.UnauthorizedApiKeys.Increment();

                return _Settings.VerboseErrorsEnabled ? 
                    throw new ApplicationException("An error occurred while validating the API key, check inner exception.", ex) 
                    : (false, new OperationError(ApiControlPlaneErrors.ApiKeyUnauthorizedForService, serviceName, operationName));
            }
        }

        _UnauthorizedApiKeyCounter.WithLabels(operationName).Inc();
        performanceMonitor.UnauthorizedApiKeys.Increment();

        return (false, new OperationError(ApiControlPlaneErrors.ApiKeyUnspecified, serviceName, operationName));
    }
}
