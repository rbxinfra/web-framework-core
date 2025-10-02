namespace Roblox.Web.Framework.Services.Http;

using System.Runtime.Serialization;

/// <summary>
/// A request model for wrapping variable request data.
/// </summary>
/// <typeparam name="TData">The request data type.</typeparam>
[DataContract]
public class RequestPayload<TData>
{
    /// <summary>
    /// The request data.
    /// </summary>
    [DataMember(Name = "data")]
    public TData Data { get; set; }
}
