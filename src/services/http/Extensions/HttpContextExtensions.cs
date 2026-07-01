namespace Roblox.Web.Framework.Services.Http;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

using Service.ApiControlPlane;

/// <summary>
/// Extension methods for <see cref="HttpContext"/>.
/// </summary>
public static class HttpContextExtensions
{
    private const string _ApiClientItemKey = "Roblox.Web.Framework.Services.Http.CurrentApiClient";
    private const string _HttpMetricsApplicationNameHeader = "Roblox-Application-Name";
    private const string _UnknownHttpMetricsApplicationName = "Unknown";

    /// <summary>
    /// Gets the current <see cref="IApiClient"/> from the <see cref="HttpContext"/>.
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
    /// <param name="context">The <see cref="HttpContext"/> to get the current <see cref="IApiClient"/> from.</param>
    /// <returns>The current <see cref="IApiClient"/>, or null if not found.</returns>
    public static IApiClient GetCurrentApiClient(this HttpContext context)
    {
        return context.Items[_ApiClientItemKey] as IApiClient;
    }

    /// <summary>
    /// Gets the requesting application name from the <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> to get the requesting application name from.</param>
    /// <returns>The requesting application name, or "Unknown" if not found.</returns>
    public static string GetRequestingApplicationName(this HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(_HttpMetricsApplicationNameHeader, out var applicationName))
        {
            return applicationName.ToString();
        }

        return _UnknownHttpMetricsApplicationName;
    }

    /// <summary>
    /// Sets the current <see cref="IApiClient"/> in the <see cref="HttpContext"/>.
    /// </summary>
    /// <remarks>
    /// This method is intended to be used by the <see cref="ValidateApiKeyAttribute"/> 
    /// to set the current <see cref="IApiClient"/> in the <see cref="HttpContext"/> after the API key has been validated.
    /// 
    /// Placed here to keep item key usage consistent and to avoid magic strings in the <see cref="ValidateApiKeyAttribute"/>.
    /// </remarks>
    /// <param name="context">The <see cref="HttpContext"/> to set the current <see cref="IApiClient"/> in.</param>
    /// <param name="apiClient">The <see cref="IApiClient"/> to set as the current <see cref="IApiClient"/> in the <see cref="HttpContext"/>.</param>
    internal static void SetCurrentApiClient(this HttpContext context, IApiClient apiClient)
    {
        context.Items[_ApiClientItemKey] = apiClient;
    }
}