using Grpc.AspNetCore.Server;

namespace Roblox.Web.Framework.Services.Grpc;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

using Prometheus;

using Instrumentation;

using GrpcPrometheus;

using StartupBase = Roblox.Web.Framework.Services.StartupBase;

/// <summary>
/// A base class for the application start up/entry.
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/startup?view=aspnetcore-5.0
/// </remarks>
public abstract class GrpcStartupBase : StartupBase
{
    /// <summary>
    /// Gets the service settings
    /// </summary>
    protected override abstract IGrpcServiceSettings Settings { get; }

    /// <summary>
    /// Configures the <see cref="IApplicationBuilder"/> for the application.
    /// </summary>
    /// <remarks>
    /// You must call this from the Configure method on startup.
    /// </remarks>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <param name="env">The <see cref="IWebHostEnvironment"/></param>
    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
            app.UseDeveloperExceptionPage();

        app.UseRouting();

        app.UseHttpMetrics(ConfigureMetrics);

        app.UseMetricServer(Settings.MetricsPort);

        app.UseEndpoints(ConfigureEndpoints);
    }

    /// <summary>
    /// Configures the <see cref="IServiceCollection"/> for the application.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddSingleton<ICounterRegistry>(_ => StaticCounterRegistry.Instance);

        services.AddSingleton<IOperationExecutor, OperationExecutor>();

        services.AddSingleton<IApiKeyParser>(ConfigureApiKeyParser(services));

        services.AddSingleton<ValidateApiKeyInterceptor>();

        services.AddGrpc(o => ConfigureGrpc(o, services));

        services.Configure<KestrelServerOptions>(ConfigureKestrelServer);
    }

    /// <summary>
    /// Configures the <see cref="KestrelServerOptions"/> for the application.
    /// </summary>
    /// <param name="options">The <see cref="KestrelServerOptions"/></param>
    protected virtual void ConfigureKestrelServer(KestrelServerOptions options)
    {
        options.ConfigureEndpointDefaults(listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http2;
        });
    }

    /// <summary>
    /// Configures <see cref="MvcOptions"/> for the application.
    /// </summary>
    /// <param name="options">The <see cref="MvcOptions"/>.</param>
    /// <param name="services">The <see cref="IServiceCollection"/></param>
    protected virtual void ConfigureGrpc(GrpcServiceOptions options, IServiceCollection services)
    {
        options.Interceptors.Add<ServerInterceptor>();
        options.Interceptors.Add<ValidateApiKeyInterceptor>();
    }

    /// <summary>
    /// Configures endpoint routing for the application.
    /// </summary>
    /// <param name="routes">The <see cref="IEndpointRouteBuilder"/>.</param>
    protected virtual void ConfigureEndpoints(IEndpointRouteBuilder routes)
    {
        routes.MapHealthChecks("/health");
    }

    /// <summary>
    /// Get an <see cref="IApiKeyParser"/> instance.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/></param>
    /// <returns>The <see cref="IApiKeyParser"/></returns>
    protected virtual IApiKeyParser ConfigureApiKeyParser(IServiceCollection services)
    {
        return new ApiKeyParser();
    }
}
