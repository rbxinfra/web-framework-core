using grpc = Grpc.Core;

namespace Roblox.Web.Framework.Services.Grpc;

using System;

/// <summary>
/// A parser for API Keys on gRPC.
/// </summary>
public interface IApiKeyParser
{
    /// <summary>
    /// Try to parse the input API key.
    /// 
    /// Can from the Roblox-Api-Key header.
    /// </summary>
    /// <param name="context">The input request.</param>
    /// <param name="apiKey">The API key.</param>
    /// <returns>True if the API key is valid.</returns>
    bool TryParseApiKey(grpc::ServerCallContext context, out Guid apiKey);
}
