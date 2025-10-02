namespace Roblox.Web.Framework.Middleware;

using System;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Controllers;

using Web.Throttling;
using Web.HttpContext;
using Web.GameServerValidation;

/// <summary>
/// An <see cref="ActionFilterAttribute"/> that throttles requests.
/// </summary>
public class ThrottlingFilterAttribute : ActionFilterAttribute
{
    private readonly IThrottlingManager _throttlingManager;
    private readonly IRequestIdentifier _requestIdentifier;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThrottlingFilterAttribute"/> class.
    /// </summary>
    /// <param name="throttlingManager">The <see cref="IThrottlingManager"/> to use for throttling.</param>
    /// <param name="requestIdentifier">The <see cref="IRequestIdentifier"/> to use for identifying the requester.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="throttlingManager"/> is <see langword="null"/>.
    /// - <paramref name="requestIdentifier"/> is <see langword="null"/>.
    /// </exception>
    public ThrottlingFilterAttribute(IThrottlingManager throttlingManager, IRequestIdentifier requestIdentifier)
    {
        _throttlingManager = throttlingManager ?? throw new ArgumentNullException(nameof(throttlingManager));
        _requestIdentifier = requestIdentifier ?? throw new ArgumentNullException(nameof(requestIdentifier));
    }

    /// <inheritdoc cref="ActionFilterAttribute.OnActionExecuting(ActionExecutingContext)"/>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var requesterType = _requestIdentifier.GetRequester(context.HttpContext);
        var originIp = context.HttpContext.GetOriginIP();
        
        // action name is the route path, ie. /v1/users
        var actionName = ((ControllerActionDescriptor)context.ActionDescriptor).ActionName;

        var requestList = _throttlingManager.GetRequestListForCurrentContext(requesterType, context.HttpContext.Request, originIp, actionName);

        if (!_throttlingManager.IsRequestAllowed(requestList, DateTime.UtcNow, requesterType, actionName))
        {
            context.Result = new StatusCodeResult(429);
            return;
        }
    }
}
