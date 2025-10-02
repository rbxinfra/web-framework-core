namespace Roblox.Web.Framework.Services.Http;

using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using EventLog;

/// <summary>
/// Middleware for logging unhandled exceptions and responding with <see cref="HttpStatusCode.InternalServerError"/>.
/// </summary>
public class UnhandledExceptionMiddleware
{
    private readonly RequestDelegate _NextHandler;
    private readonly ILogger _Logger;

    private readonly IServiceSettings _Settings;

    /// <summary>
    /// Initializes a new <see cref="UnhandledExceptionMiddleware"/>.
    /// </summary>
    /// <param name="nextHandler">A delegate for triggering the next handler.</param>
    /// <param name="logger">An <see cref="ILogger"/>.</param>
    /// <param name="settings">The <see cref="IServiceSettings"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="nextHandler"/>
    /// - <paramref name="logger"/>
    /// - <paramref name="settings"/>
    /// </exception>
    public UnhandledExceptionMiddleware(RequestDelegate nextHandler, ILogger logger, IServiceSettings settings)
    {
        _NextHandler = nextHandler ?? throw new ArgumentNullException(nameof(nextHandler));
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// The method to invoke the handler.
    /// </summary>
    /// <param name="context">An <see cref="HttpContext"/>.</param>
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _NextHandler(context);
        }
        catch (Exception ex)
        {
            _Logger.Error(ex);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            if (_Settings.VerboseErrorsEnabled)
            {
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync(ex.ToString());
            }
        }
    }
}
