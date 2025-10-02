namespace Roblox.Web.Framework.Middleware;

using System;
using System.Net;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using Prometheus;

using Metrics;

/// <summary>
/// Middleware to handle the response
/// </summary>
/// <summary>
/// Middleware for counting and recording http response metrics.
/// </summary>
public sealed class HttpServerResponseMiddleware : HttpServerMiddlewareBase
{
    private readonly RequestDelegate _next;
    private readonly IWebResponseMetricsFactory _webResponseMetricsFactory;

    private readonly Counter _HttpResponseCounter;
    private readonly Histogram _RequestDurationHistogram;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServerResponseMiddleware" /> class.
    /// </summary>
    /// <param name="next">The <see cref="RequestDelegate" />.</param>
    /// <param name="webResponseMetricsFactory">The <see cref="IWebResponseMetricsFactory" />.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="next" /> cannot be null.
    /// - <paramref name="webResponseMetricsFactory" /> cannot be null.
    /// </exception>
    public HttpServerResponseMiddleware(RequestDelegate next, IWebResponseMetricsFactory webResponseMetricsFactory)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _webResponseMetricsFactory = webResponseMetricsFactory ?? throw new ArgumentNullException(nameof(webResponseMetricsFactory));

        _HttpResponseCounter = Metrics.CreateCounter(
            "http_server_response_total",
            "Total number of http responses",
            "Method",
            "Endpoint",
            "StatusCode"
        );

        _RequestDurationHistogram = Metrics.CreateHistogram(
            "http_server_request_duration_seconds",
            "Duration in seconds each request takes",
            "Method",
            "Endpoint"
        );
    }

    /// <summary>
    /// Invokes the middleware
    /// </summary>
    /// <param name="context">The <see cref="HttpContext" />.</param>
    /// <returns>A <see cref="Task" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> cannot be null.</exception>
    public async Task Invoke(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var latencyStopwatch = Stopwatch.StartNew();
        var (controller, action) = GetControllerAndAction(context);
        var endpoint = controller != _UnknownRouteLabelValue && action != _UnknownRouteLabelValue ?
            string.Format("{0}.{1}", controller, action)
            : _UnknownRouteLabelValue;

        try
        {
            await _next(context).ConfigureAwait(false);

            context.Response.OnCompleted(() =>
            {
                latencyStopwatch.Stop();

                var statusCode = context.Response.StatusCode;

                var endpointResponsePerformanceCounter = _webResponseMetricsFactory.GetEndpointResponsePerformanceCounter(controller, action);
                var statusCodePerformanceCounter = _webResponseMetricsFactory.GetHttpStatusCodePerformanceCounter((HttpStatusCode)statusCode);

                if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
                {
                    endpointResponsePerformanceCounter.Increment(latencyStopwatch.Elapsed);
                    _RequestDurationHistogram.WithLabels(context.Request.Method, endpoint).Observe(latencyStopwatch.Elapsed.TotalSeconds);
                }

                _HttpResponseCounter.WithLabels(context.Request.Method, endpoint, context.Response.StatusCode.ToString()).Inc();

                statusCodePerformanceCounter.Increment();

                return Task.CompletedTask;
            });
        }
        catch (Exception)
        {
            _HttpResponseCounter.WithLabels(context.Request.Method, endpoint, "500").Inc();

            latencyStopwatch.Stop();

            var statusCode = context.Response.StatusCode;

            var endpointFailurePerformanceCounter = _webResponseMetricsFactory.GetEndpointFailurePerformanceCounter(controller, action);
            var statusCodePerformanceCounter = _webResponseMetricsFactory.GetHttpStatusCodePerformanceCounter((HttpStatusCode)statusCode);

            endpointFailurePerformanceCounter.Increment(latencyStopwatch.Elapsed);
            statusCodePerformanceCounter.Increment();

            throw;
        }
    }
}
