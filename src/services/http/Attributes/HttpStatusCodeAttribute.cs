namespace Roblox.Web.Framework.Services.Http;

using System;
using System.Net;

/// <summary>
/// An attribute that can be used to specify
/// an HTTP status code on an <see cref="Operations.OperationError"/>
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class HttpStatusCodeAttribute : Attribute
{
    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Construct a new instance of <see cref="HttpStatusCodeAttribute"/>
    /// </summary>
    /// <param name="statusCode">The HTTP status code number, defaults to 400.</param>
    public HttpStatusCodeAttribute(int statusCode = 400)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Construct a new instance of <see cref="HttpStatusCodeAttribute"/>
    /// </summary>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/>, defaults to <see cref="HttpStatusCode.BadRequest"/></param>
    public HttpStatusCodeAttribute(HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        : this((int)statusCode)
    {
    }
}