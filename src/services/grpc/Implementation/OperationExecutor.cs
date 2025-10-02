using grpc = Grpc.Core;

namespace Roblox.Web.Framework.Services.Grpc;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Operations;

/// <inheritdoc cref="IOperationExecutor"/>
public class OperationExecutor : IOperationExecutor
{
    /// <inheritdoc cref="IOperationExecutor.Execute(IOperation)"/>
    public void Execute(IOperation action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var error = action.Execute();
        if (error == null) return;

        ThrowError(error);
    }

    /// <inheritdoc cref="IOperationExecutor.Execute{TInput}(IOperation{TInput}, TInput)"/>
    public void Execute<TInput>(IOperation<TInput> action, TInput input)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var error = action.Execute(input);
        if (error == null) return;

        ThrowError(error);
    }

    /// <inheritdoc cref="IOperationExecutor.Execute{TOutput}(IResultOperation{TOutput})"/>
    public TOutput Execute<TOutput>(IResultOperation<TOutput> operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var (data, operationError) = operation.Execute();
        if (operationError == null) return data;

        ThrowError(operationError);

        return default(TOutput);
    }

    /// <inheritdoc cref="IOperationExecutor.Execute{TInput,TOutput}(IResultOperation{TInput,TOutput}, TInput)"/>
    public TOutput Execute<TInput, TOutput>(IResultOperation<TInput, TOutput> operation, TInput input)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var (data, operationError) = operation.Execute(input);
        if (operationError == null) return data;

        ThrowError(operationError);

        return default(TOutput);
    }

    /// <inheritdoc cref="IOperationExecutor.ExecuteAsync(IAsyncOperation, CancellationToken)"/>
    public async Task ExecuteAsync(IAsyncOperation action, CancellationToken cancellationToken)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var error = await action.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        if (error == null) return;

        ThrowError(error);
    }

    /// <inheritdoc cref="IOperationExecutor.ExecuteAsync{TInput}(IAsyncOperation{TInput}, TInput, CancellationToken)"/>
    public async Task ExecuteAsync<TInput>(IAsyncOperation<TInput> action, TInput input, CancellationToken cancellationToken)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var error = await action.ExecuteAsync(input, cancellationToken).ConfigureAwait(false);
        if (error == null) return;

        ThrowError(error);
    }

    /// <inheritdoc cref="IOperationExecutor.ExecuteAsync{TOutput}(IAsyncResultOperation{TOutput}, CancellationToken)"/>
    public async Task<TOutput> ExecuteAsync<TOutput>(IAsyncResultOperation<TOutput> operation, CancellationToken cancellationToken)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var (data, operationError) = await operation.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        if (operationError == null) return data;

        ThrowError(operationError);

        return default(TOutput);
    }

    /// <inheritdoc cref="IOperationExecutor.ExecuteAsync{TInput,TOutput}(IAsyncResultOperation{TInput,TOutput}, TInput, CancellationToken)"/>
    public async Task<TOutput> ExecuteAsync<TInput, TOutput>(IAsyncResultOperation<TInput, TOutput> operation, TInput input, CancellationToken cancellationToken)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var (data, operationError) = await operation.ExecuteAsync(input, cancellationToken).ConfigureAwait(false);
        if (operationError == null) return data;

        ThrowError(operationError);

        return default(TOutput);
    }

    private static IActionResult ThrowError(OperationError operationError)
    {
        var message = operationError.Message ?? operationError.Code;

        // Message is the status description.
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.InvalidArgument, message));
    }
}
