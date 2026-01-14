namespace Roblox.Web.Framework.Common;

using System;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Represents an HTTP status code result.
/// </summary>
/// <remarks>
/// This class is different from <see cref="StatusCodeResult"/> in that it allows changing
/// the reason phrase of the status code.
/// </remarks>
public class HttpStatusCodeResult : ActionResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpStatusCodeResult"/> class.
    /// </summary>
    /// <param name="statusCode">The status code.</param>
    /// <param name="reasonPhrase">The reason phrase.</param>
    public HttpStatusCodeResult(int statusCode, string reasonPhrase = null)
    {
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase;
    }

    /// <summary>
    /// Gets or sets the status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the reason phrase.
    /// </summary>
    public string ReasonPhrase { get; set; }

    /// <inheritdoc/>
    public override void ExecuteResult(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var response = context.HttpContext.Response;

        response.StatusCode = StatusCode;
        
        if (!string.IsNullOrEmpty(ReasonPhrase))
            context.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = ReasonPhrase;
    }
}
