using grpc = Grpc.Core;

namespace Roblox.Web.Framework.Services.Grpc;

using System;

/// <summary>
/// An <see cref="IApiKeyParser"/> that parses the ApiKey from a request header.
/// </summary>
/// <seealso cref="IApiKeyParser"/>
public class ApiKeyParser : IApiKeyParser
{
    /// <summary>
    /// The name of the api key header
    /// </summary>
    public const string ApiKeyHeaderName = "Roblox-Api-Key";

    /// <inheritdoc cref="IApiKeyParser.TryParseApiKey"/>
    public bool TryParseApiKey(grpc::ServerCallContext request, out Guid apiKey)
    {
        apiKey = Guid.Empty;

        var metadata = request.RequestHeaders.Get(ApiKeyHeaderName);
        return metadata != null && Guid.TryParse(metadata.Value, out apiKey);
    }
}
