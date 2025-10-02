namespace Roblox.Web.Framework.Services.Http;

using System.Runtime.Serialization;

/// <summary>
/// A model to wrap response data.
/// </summary>
/// <typeparam name="TData">The response data type.</typeparam>
[DataContract]
public class Payload<TData>
{
    /// <summary>
    /// The response data.
    /// </summary>
    [DataMember(Name = "data")]
    public TData Data { get; set; }

    /// <summary>
    /// Initializes a new <see cref="Payload{TData}"/>.
    /// </summary>
    /// <param name="data">The <see cref="Data"/>.</param>
    public Payload(TData data)
    {
        Data = data;
    }
}
