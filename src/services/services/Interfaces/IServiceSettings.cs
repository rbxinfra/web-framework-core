namespace Roblox.Web.Framework.Services;

using EventLog;

/// <summary>
/// Settings for a service.
/// </summary>
public interface IServiceSettings
{
    /// <summary>
    /// Gets the API key for the service. 
    /// Used for access to API control plane.
    /// </summary>
    string ApiKey { get; }

    /// <summary>
    /// Gets the log level for the service.
    /// </summary>
    public LogLevel LogLevel { get; }

    /// <summary>
    /// Determines wether or not errors are thrown back to
    /// the client.
    /// </summary>
    public bool VerboseErrorsEnabled { get; }
}
