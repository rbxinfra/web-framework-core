using grpc = Grpc.Core;

namespace Roblox.Web.Framework.Services.Grpc;

using System;

/// <summary>
/// An attribute that can be used to specify
/// a <see cref="grpc::StatusCode"/> on an <see cref="Operations.OperationError"/>
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class GrpcStatusCodeAttribute : Attribute
{
    /// <summary>
    /// Gets the <see cref="grpc::StatusCode"/>
    /// </summary>
    public grpc::StatusCode StatusCode { get; }

    /// <summary>
    /// Construct a new instance of <see cref="GrpcStatusCodeAttribute"/>
    /// </summary>
    /// <param name="statusCode">The <see cref="grpc::StatusCode"/>, by default <see cref="grpc::StatusCode.InvalidArgument"/></param>
    public GrpcStatusCodeAttribute(grpc::StatusCode statusCode = grpc::StatusCode.InvalidArgument)
    {
        StatusCode = statusCode;
    }
}