namespace Roblox.Web.Framework.Services.Http;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json.Converters;

using Prometheus;

using Instrumentation;
using Serialization.Json;
using ApplicationContext;


using Roblox.Web.Metrics;
using Roblox.Web.Framework.Middleware;
using Roblox.Web.Documentation.Services;

using StartupBase = Roblox.Web.Framework.Services.StartupBase;

/// <summary>
/// A base class for the application start up/entry.
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/startup?view=aspnetcore-5.0
/// </remarks>
public abstract class HttpStartupBase : StartupBase
{
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
	app.UseRequestDecompression();

        if (env.IsDevelopment())
            app.UseDeveloperExceptionPage();

        app.UseRouting();

        app.UseHttpMetrics(ConfigureMetrics);

        app.UseMetricServer();
        app.UseMiddleware<UnhandledExceptionMiddleware>();
        app.UseTelemetry();
        app.UseSwagger(ApplicationContext.Singleton);

        app.UseEndpoints(ConfigureEndpoints);
    }

    /// <summary>
    /// Configures the <see cref="IServiceCollection"/> for the application.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

	    services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
            options.SuppressConsumesConstraintForFormFileParameters = true;
        });

        services.AddRequestDecompression();

        services.AddSingleton<ICounterRegistry>(_ => StaticCounterRegistry.Instance);
        services.AddSingleton<IWebResponseMetricsFactory>(provider => new WebResponseMetricsFactory(
            provider.GetRequiredService<ICounterRegistry>(), 
            provider.GetRequiredService<IApplicationContext>().Name
        ));

        services.AddSingleton<IOperationExecutor, OperationExecutor>();

        services.AddSingleton<IApiKeyParser>(ConfigureApiKeyParser(services));

        services.AddSingleton<ValidateApiKeyAttribute>();

        services.AddControllers(ConfigureMvc)
            .AddNewtonsoftJson(ConfigureJson);

        services.AddSwagger();
    }

    /// <summary>
    /// Configures <see cref="JsonOptions"/> for the application.
    /// </summary>
    /// <param name="options">The <see cref="JsonOptions"/>.</param>
    protected virtual void ConfigureJson(MvcNewtonsoftJsonOptions options)
    {
        options.SerializerSettings.Converters.Add(new StringEnumConverter());
        options.SerializerSettings.Converters.Add(new KindAwareDateTimeConverter());
    }

    /// <summary>
    /// Configures <see cref="MvcOptions"/> for the application.
    /// </summary>
    /// <param name="options">The <see cref="MvcOptions"/>.</param>
    protected virtual void ConfigureMvc(MvcOptions options)
    {
        options.Filters.Add(new ProducesAttribute("application/json"));
        options.Filters.Add<ValidateApiKeyAttribute>();
    }

    /// <summary>
    /// Configures endpoint routing for the application.
    /// </summary>
    /// <param name="endpointRouteBuilder">The <see cref="IEndpointRouteBuilder"/>.</param>
    protected virtual void ConfigureEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapHealthChecks("/health");

        endpointRouteBuilder.MapControllers();
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
