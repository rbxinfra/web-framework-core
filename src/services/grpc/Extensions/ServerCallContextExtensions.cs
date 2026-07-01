using Grpc.Core;

namespace Roblox.Web.Framework.Services.Grpc;

using Microsoft.AspNetCore.Authorization;

using Service.ApiControlPlane;

/// <summary>
/// Extension methods for <see cref="ServerCallContext"/>.
/// </summary>
public static class ServerCallContextExtensions
{
    private const string _ApiClientUserStateKey = "Roblox.Web.Framework.Services.Grpc.CurrentApiClient";
    private const string _UnknownGrpcMetricsApplicationName = "Unknown";
    private const string _GrpcMetricsApplicationNameHeader = "Roblox-Application-Name";

    /// <summary>
    /// Gets the current <see cref="IApiClient"/> from the <see cref="ServerCallContext"/>.
    /// </summary>
    /// <remarks>
    /// This method will return null if:
    /// <list type="bullet">
    /// <item>The operation being executed has the <see cref="AllowAnonymousAttribute"/> applied to it.</item>
    /// <item>The operation being executed does not have an API key provided in the request.</item>
    /// <item>The API key provided in the request is not associated with a known client.</item>
    /// <item>The API key provided in the request is not authorized for the service or operation being executed.</item>
    /// </list>
    /// <para>
    /// The last 3 cases won't be a problem anyway because execution shouldn't
    /// be passed to the operation if the API key is not valid, but this method will return null in those cases anyway.
    /// </para>
    /// </remarks>
    /// <param name="context">The <see cref="ServerCallContext"/> to get the current <see cref="IApiClient"/> from.</param>
    /// <returns>The current <see cref="IApiClient"/>, or null if not found.</returns>
    public static IApiClient GetCurrentApiClient(this ServerCallContext context)
        => context.UserState.TryGetValue(_ApiClientUserStateKey, out var apiClient) ? apiClient as IApiClient : null;

    /// <summary>
    /// Gets the requesting application name from the <see cref="ServerCallContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="ServerCallContext"/> to get the requesting application name from.</param>
    /// <returns>The requesting application name, or "Unknown" if not found.</returns>
    public static string GetRequestingApplicationName(this ServerCallContext context)
        => context.RequestHeaders.Get(_GrpcMetricsApplicationNameHeader)?.Value ?? _UnknownGrpcMetricsApplicationName;

    /// <summary>
    /// Sets the current <see cref="IApiClient"/> in the <see cref="ServerCallContext"/>.
    /// </summary>
    /// <remarks>
    /// This method is intended to be used by the <see cref="ValidateApiKeyInterceptor"/> 
    /// to set the current <see cref="IApiClient"/> in the <see cref="ServerCallContext"/> after the API key has been validated.
    /// 
    /// Placed here to keep user state key usage consistent and to avoid magic strings in the <see cref="ValidateApiKeyInterceptor"/>.
    /// </remarks>
    /// <param name="context">The <see cref="ServerCallContext"/> to set the current <see cref="IApiClient"/> in.</param>
    /// <param name="apiClient">The <see cref="IApiClient"/> to set as the current <see cref="IApiClient"/> in the <see cref="ServerCallContext"/>.</param>
    internal static void SetCurrentApiClient(this ServerCallContext context, IApiClient apiClient)
        => context.UserState[_ApiClientUserStateKey] = apiClient;
}