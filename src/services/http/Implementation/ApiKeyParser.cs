namespace Roblox.Web.Framework.Services.Http;

using System;
using System.Linq;

using HttpRequest = Microsoft.AspNetCore.Http.HttpRequest;

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

    private const string _ApiKeyQueryName = "apiKey";

    private readonly bool _ApiKeyValidationViaHeader;

    /// <summary>
    /// Initializes a new <see cref="ApiKeyParser"/>.
    /// </summary>
    /// <param name="apiKeyValidationViaHeader">Should we validate the apiKey based on the header?</param>
    public ApiKeyParser(bool apiKeyValidationViaHeader)
    {
        _ApiKeyValidationViaHeader = apiKeyValidationViaHeader;
    }

    /// <inheritdoc cref="IApiKeyParser.TryParseApiKey"/>
    public bool TryParseApiKey(HttpRequest request, out Guid apiKey)
    {
        apiKey = default;

        if (_ApiKeyValidationViaHeader)
            return request.Headers.TryGetValue(ApiKeyHeaderName, out var rawApiKey)
                && Guid.TryParse(rawApiKey, out apiKey);

        return request.Query.TryGetValue(_ApiKeyQueryName, out var values)
            && Guid.TryParse(values.FirstOrDefault(), out apiKey);
    }
}
