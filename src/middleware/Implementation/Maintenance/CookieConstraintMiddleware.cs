namespace Roblox.Web.Framework.Middleware;

using System;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Web.Maintenance;
using System.Threading.Tasks;
using System.Net;

/// <summary>
/// A middleware that manages the cookie constraint.
/// </summary>
public class CookieConstraintMiddleware 
{
    private readonly RequestDelegate _NextHandler;
    private readonly ICookieConstraintVerifier _CookieConstraintVerifier;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThrottlingFilterAttribute"/> class.
    /// </summary>
    /// <param name="nextHandler">A delegate for triggering the next handler.</param>
    /// <param name="cookieConstraintVerifier">The <see cref="ICookieConstraintVerifier"/> to use for verifying maintenance.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="nextHandler"/> is <see langword="null"/>.
    /// - <paramref name="cookieConstraintVerifier"/> is <see langword="null"/>.
    /// </exception>
    public CookieConstraintMiddleware(RequestDelegate nextHandler, ICookieConstraintVerifier cookieConstraintVerifier)
    {
        _NextHandler = nextHandler ?? throw new ArgumentNullException(nameof(nextHandler));
        _CookieConstraintVerifier = cookieConstraintVerifier ?? throw new ArgumentNullException(nameof(cookieConstraintVerifier));
    }

    /// <summary>
    /// The method to invoke the handler.
    /// </summary>
    /// <param name="context">An <see cref="HttpContext"/>.</param>
    public async Task Invoke(HttpContext context)
    {
        if (!_CookieConstraintVerifier.IsVerified(context))
        {
            context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            await context.Response.CompleteAsync();

            return;
        }

        await _NextHandler(context);
    }
}
