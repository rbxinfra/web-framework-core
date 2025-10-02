namespace Roblox.Web.Framework.Services.Grpc;

using EventLog;

/// <summary>
/// Settings for a service.
/// </summary>
public interface IGrpcServiceSettings : IServiceSettings
{
    /// <summary>
    /// Gets port for the metrics server.
    /// </summary>
    int MetricsPort { get; }
}
