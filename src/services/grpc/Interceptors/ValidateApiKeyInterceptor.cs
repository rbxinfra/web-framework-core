using grpc_interceptors = Grpc.Core.Interceptors;

using Grpc.Core;
using Grpc.AspNetCore.Server;

namespace Roblox.Web.Framework.Services.Grpc;

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

using Prometheus;

using EventLog;
using Instrumentation;
using ApplicationContext;

using Api.ControlPlane;
using Operations.Monitoring;

/// <summary>
/// An <see cref="grpc_interceptors::Interceptor"/> that validates an Api Key
/// </summary>
public class ValidateApiKeyInterceptor : grpc_interceptors::Interceptor
{
    private readonly IApplicationContext _ApplicationContext;
    private readonly IApiKeyParser _ApiKeyParser;
    private readonly IAuthority _Authority;
    private readonly ILogger _Logger;
    private readonly ICounterRegistry _CounterRegistry;
    private readonly IServiceSettings _Settings;

    private const string _ApiClientUserStateKey = "ApiClient";
    private const string _UnknownHttpMetricsApplicationName = "Unknown";
    private const string _ApplicationNameHeader = "Roblox-Application-Name";
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
    /// Create a new instance of <see cref="ValidateApiKeyInterceptor"/>
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
    public ValidateApiKeyInterceptor(
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

    /// <inheritdoc cref="grpc_interceptors::Interceptor.UnaryServerHandler{TRequest, TResponse}(TRequest, ServerCallContext, UnaryServerMethod{TRequest, TResponse})"/>
    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation
    )
    {
        if (!ShouldValidateApiKey(context)) return continuation(request, context);

        Validate(context);

        return continuation(request, context);
    }

    /// <inheritdoc cref="grpc_interceptors::Interceptor.ServerStreamingServerHandler{TRequest, TResponse}(TRequest, IServerStreamWriter{TResponse}, ServerCallContext, ServerStreamingServerMethod{TRequest, TResponse})"/>
    public override Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation
    )
    {
        if (!ShouldValidateApiKey(context)) return continuation(request, responseStream, context);

        Validate(context);

        return continuation(request, responseStream, context);
    }

    /// <inheritdoc cref="grpc_interceptors::Interceptor.ClientStreamingServerHandler{TRequest, TResponse}(IAsyncStreamReader{TRequest}, ServerCallContext, ClientStreamingServerMethod{TRequest, TResponse})"/>
    public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation
    )
    {
        if (!ShouldValidateApiKey(context)) return continuation(requestStream, context);

        Validate(context);

        return continuation(requestStream, context);
    }

    /// <inheritdoc cref="grpc_interceptors::Interceptor.DuplexStreamingServerHandler{TRequest, TResponse}(IAsyncStreamReader{TRequest}, IServerStreamWriter{TResponse}, ServerCallContext, DuplexStreamingServerMethod{TRequest, TResponse})"/>
    public override Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation
    )
    {
        if (!ShouldValidateApiKey(context)) return continuation(requestStream, responseStream, context);

        Validate(context);

        return continuation(requestStream, responseStream, context);
    }

    private void Validate(ServerCallContext context)
    {
        var serviceName = _ApplicationContext.Name;
        var operationName = context.Method.Split('/').Last();

        if (!_Authority.ServiceIsEnabled(serviceName) || !_Authority.OperationIsEnabled(serviceName, operationName))
            throw new RpcException(new(StatusCode.Unavailable, $"Service {serviceName} or operation {operationName} is disabled in ApiControlPlane"));

        var performanceMonitor = _PerOperationApiKeyPerformanceMonitors.GetOrAdd(operationName, new PerOperationApiKeyPerformanceMonitor(_CounterRegistry, operationName));

        ValidateApiKey(serviceName, operationName, context, performanceMonitor);
    }

    private static bool ShouldValidateApiKey(ServerCallContext context)
    {
        var metadata = context.GetHttpContext().GetEndpoint()?.Metadata[0];
        if (metadata is not GrpcMethodMetadata methodMetadata) return false;

        var service = methodMetadata.ServiceType;
        var method = service.GetMethod(methodMetadata.Method.Name, BindingFlags.Public | BindingFlags.Instance);
        if (method == null) return false;

        var allowAnonymousAttributes = method.GetCustomAttributes(
                attributeType: _AnonymousAttributeType,
                inherit: true).ToList();

        allowAnonymousAttributes.AddRange(service.GetCustomAttributes(
                attributeType: _AnonymousAttributeType,
                inherit: true));

        return allowAnonymousAttributes.Count == 0;
    }

    private static string GetApplicationName(ServerCallContext context)
        => context.RequestHeaders.Get(_ApplicationNameHeader)?.Value ?? _UnknownHttpMetricsApplicationName;

    private void ValidateApiKey(string serviceName, string operationName, ServerCallContext context, PerOperationApiKeyPerformanceMonitor performanceMonitor)
    {
        if (
            _ApiKeyParser.TryParseApiKey(
                context,
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

                    throw new RpcException(new(StatusCode.PermissionDenied, $"ApiKey ({client?.Note ?? _UnknownClientMessage}) is not authorized for service {serviceName} or operation {operationName} in ApiControlPlane."));
                }

                _AuthorizedApiKeyCounter.WithLabels(operationName, GetApplicationName(context), client.Note).Inc();
                performanceMonitor.AuthorizedApiKeys.Increment();

                context.UserState.Add(_ApiClientUserStateKey, client);

                return;
            }
            catch (Exception ex) when (ex is not RpcException)
            {
                _Logger.Error(ex);

                _UnauthorizedApiKeyCounter.WithLabels(operationName).Inc();
                performanceMonitor.UnauthorizedApiKeys.Increment();

                if (_Settings.VerboseErrorsEnabled)
                    throw new RpcException(new(StatusCode.Internal, "An error occurred while validating the API key, check inner exception.", ex));
                
                throw new RpcException(new(StatusCode.PermissionDenied, $"ApiKey ({_UnknownClientMessage}) is not authorized for service {serviceName} or operation {operationName} in ApiControlPlane."));
            }
        }

        _UnauthorizedApiKeyCounter.WithLabels(operationName).Inc();
        performanceMonitor.UnauthorizedApiKeys.Increment();

        throw new RpcException(new(StatusCode.Unauthenticated, $"API key ({ApiKeyParser.ApiKeyHeaderName}) not specified in request to {serviceName} ({operationName})"));
    }
}
