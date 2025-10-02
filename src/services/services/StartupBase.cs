namespace Roblox.Web.Framework.Services;

using System;
using System.Linq;
using System.Reflection;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Prometheus;
using Prometheus.HttpMetrics;

using EventLog;
using Configuration;
using ApplicationContext;
using ApplicationTelemetry;

using Roblox.RequestContext;

using Api.ControlPlane;
using Api.ControlPlane.Factories;
using Http.Client.ApiControlPlane;

using Roblox.Web.RequestContext.ServiceContextLoader;

/// <summary>
/// A base class for the application start up/entry.
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/startup?view=aspnetcore-5.0
/// </remarks>
public abstract class StartupBase
{
    /// <summary>
    /// Gets the service settings
    /// </summary>
    protected abstract IServiceSettings Settings { get; }

    /// <summary>
    /// Configures the <see cref="IApplicationBuilder"/> for the application.
    /// </summary>
    /// <remarks>
    /// You must call this from the Configure method on startup.
    /// </remarks>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <param name="env">The <see cref="IWebHostEnvironment"/></param>
    public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }

    /// <summary>
    /// Configures the <see cref="IServiceCollection"/> for the application.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    public virtual void ConfigureServices(IServiceCollection services)
    {
	services.AddSingleton<IServiceSettings>(Settings);	

        var apiControlPlaneClient = new ApiControlPlaneClient(
            () => RobloxEnvironment.GetInternalApiServiceEndpoint("apicontrolplane"),
            () => Settings.ApiKey
        );

        services.AddSingleton<IApiControlPlaneClient>(apiControlPlaneClient);

        services.AddHttpContextAccessor();

        services.AddTransient<IRequestContextLoader, ServiceContextLoader>();

        var applicationContext = ApplicationContext.Singleton;

        services.AddSingleton<IApplicationContext>(applicationContext);

        services.AddSingleton<ILogger>(ConfigureLogger);

        services.AddSingleton<ITelemetry, Telemetry>();

        services.AddSingleton<IServiceRegistrationFactory, ServiceRegistrationFactory>();
        services.AddSingleton<IAuthorizationVerifier, AuthorizationVerifier>();

        services.AddSingleton<ServiceAuthority>();

        services.AddSingleton<IAuthority>(ConfigureAuthority);

        services.AddHealthChecks();


        var metadataAttributes = applicationContext.Assembly.GetCustomAttributes<AssemblyMetadataAttribute>();

        var dockerTag = metadataAttributes.FirstOrDefault(a => a.Key == "DockerTag")?.Value ?? "Unknown";
        var serviceVersion = metadataAttributes.FirstOrDefault(a => a.Key == "GitHash")?.Value ?? "Unknown";

        Metrics.CreateGauge(
            "service_startup_info",
            "Service startup information",
            "service_name",
            "service_version",
            "docker_tag"
        ).WithLabels(
            applicationContext.Name,
            serviceVersion,
            dockerTag
        ).Set(1);
    }

    /// <summary>
    /// Get the default logger
    /// </summary>
    /// <param name="provider">The <see cref="IServiceProvider"/></param>
    /// <returns>The <see cref="ILogger"/> object.</returns>
    protected virtual ILogger ConfigureLogger(IServiceProvider provider)
    {
        return new Logger(
            provider.GetRequiredService<IApplicationContext>().Name.ToLowerInvariant(), 
            () => Settings.LogLevel
        );
    }

    /// <summary>
    /// Configure HTTP metrics exported to prometheus.
    /// </summary>
    /// <param name="options">The <see cref="HttpMiddlewareExporterOptions"/>.</param>
    protected virtual void ConfigureMetrics(HttpMiddlewareExporterOptions options)
    {
    }

    /// <summary>
    /// Get an <see cref="IAuthority"/> instance.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/></param>
    /// <returns>The <see cref="IAuthority"/></returns>
    protected virtual IAuthority ConfigureAuthority(IServiceProvider services)
    {
        return services.GetRequiredService<ServiceAuthority>();
    }
}
