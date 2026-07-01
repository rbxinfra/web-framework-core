namespace Roblox.Web.Framework.Services.Http;

#if DEBUG
using System.ComponentModel;
#endif

/// <summary>
/// Defines the error codes for the API Control Plane service.
/// </summary>
/// <remarks>
/// Use the debug build of Web.Framework to get verbose error messages for API Control Plane errors. 
/// The debug build of Web.Framework will include the <see cref="DescriptionAttribute"/> on the enum values, 
/// which will provide a more detailed error message.
/// 
/// The messages specified are the legacy messages from prior ApiControlPlane builds.
/// </remarks>
public enum ApiControlPlaneErrors
{
    /// <summary>
    /// The service is disabled and cannot be used.
    /// </summary>
#if DEBUG
    [Description("Service ({0}) is disabled.")]
#endif
    ServiceDisabled,

    /// <summary>
    /// The operation is disabled and cannot be used.
    /// </summary>
#if DEBUG
    [Description("Operation ({0}) is disabled (on service: {1}).")]
#endif
    OperationDisabled,

    /// <summary>
    /// The API key was not provided in the request.
    /// </summary>
#if DEBUG
    [Description($"API key ({ApiKeyParser.ApiKeyHeaderName}) not specified in request to {{0}} ({{1}})")]
#endif
    ApiKeyUnspecified,

    /// <summary>
    /// The API key provided in the request and it either:
    /// <list type="number">
    /// <item>Has not associated with a known client, or</item>
    /// <item>Has no authorizations for the service being requested.</item>
    /// </list>
    /// </summary>
#if DEBUG
    [Description("Client (API Key was passed but isn't associated with a known client) is not authorized for {0} (on service: {1})")]
#endif
    ApiKeyUnauthorizedForService,

    /// <summary>
    /// The API key provided in the request, has an authorization for the service being requested, 
    /// but does not have an authorization for the operation being requested.
    /// </summary>
#if DEBUG
    [Description("Client ({0}) is not authorized for {1} (on service: {2})")]
#endif
    ApiKeyUnauthorizedForOperation
}