namespace Roblox.Web.Framework.Services.Http;

using System;

using HttpRequest = Microsoft.AspNetCore.Http.HttpRequest;

/// <summary>
/// A parser for API Keys.
/// </summary>
public interface IApiKeyParser
{
    /// <summary>
    /// Try to parse the input API key.
    /// 
    /// Can either be from query string or Roblox-Api-Key header.
    /// </summary>
    /// <param name="request">The input request.</param>
    /// <param name="apiKey">The API key.</param>
    /// <returns>True if the API key is valid.</returns>
    bool TryParseApiKey(HttpRequest request, out Guid apiKey);
}
