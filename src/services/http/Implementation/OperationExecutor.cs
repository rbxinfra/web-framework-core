namespace Roblox.Web.Framework.Services.Http;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Operations;
using Framework.Common;

/// <inheritdoc cref="IOperationExecutor"/>
public class OperationExecutor : IOperationExecutor
{
    /// <inheritdoc cref="IOperationExecutor.Execute(IOperation)"/>
    public IActionResult Execute(IOperation action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var error = action.Execute();
        if (error != null)
            return BuildErrorResult(error);

        return new NoContentResult();
    }

    /// <inheritdoc cref="IOperationExecutor.Execute{TInput}(IOperation{TInput}, TInput)"/>
    public IActionResult Execute<TInput>(IOperation<TInput> action, TInput input)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var error = action.Execute(input);
        if (error != null)
            return BuildErrorResult(error);

        return new NoContentResult();
    }

    /// <inheritdoc cref="IOperationExecutor.Execute{TOutput}(IResultOperation{TOutput})"/>
    public IActionResult Execute<TOutput>(IResultOperation<TOutput> operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var (data, operationError) = operation.Execute();
        return BuildPayloadResult(data, operationError);
    }

    /// <inheritdoc cref="IOperationExecutor.Execute{TInput,TOutput}(IResultOperation{TInput,TOutput}, TInput)"/>
    public IActionResult Execute<TInput, TOutput>(IResultOperation<TInput, TOutput> operation, TInput input)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var (data, operationError) = operation.Execute(input);
        return BuildPayloadResult(data, operationError);
    }

    /// <inheritdoc cref="IOperationExecutor.ExecuteAsync(IAsyncOperation, CancellationToken)"/>
    public async Task<IActionResult> ExecuteAsync(IAsyncOperation action, CancellationToken cancellationToken)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var error = await action.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        if (error != null)
            return BuildErrorResult(error);

        return new NoContentResult();
    }

    /// <inheritdoc cref="IOperationExecutor.ExecuteAsync{TInput}(IAsyncOperation{TInput}, TInput, CancellationToken)"/>
    public async Task<IActionResult> ExecuteAsync<TInput>(IAsyncOperation<TInput> action, TInput input, CancellationToken cancellationToken)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var error = await action.ExecuteAsync(input, cancellationToken).ConfigureAwait(false);
        if (error != null)
            return BuildErrorResult(error);

        return new NoContentResult();
    }

    /// <inheritdoc cref="IOperationExecutor.ExecuteAsync{TOutput}(IAsyncResultOperation{TOutput}, CancellationToken)"/>
    public async Task<IActionResult> ExecuteAsync<TOutput>(IAsyncResultOperation<TOutput> operation, CancellationToken cancellationToken)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var (data, operationError) = await operation.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        return BuildPayloadResult(data, operationError);
    }

    /// <inheritdoc cref="IOperationExecutor.ExecuteAsync{TInput,TOutput}(IAsyncResultOperation{TInput,TOutput}, TInput, CancellationToken)"/>
    public async Task<IActionResult> ExecuteAsync<TInput, TOutput>(IAsyncResultOperation<TInput, TOutput> operation, TInput input, CancellationToken cancellationToken)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var (data, operationError) = await operation.ExecuteAsync(input, cancellationToken).ConfigureAwait(false);
        return BuildPayloadResult(data, operationError);
    }

    private static IActionResult BuildPayloadResult<TData>(TData data, OperationError operationError)
    {
        if (operationError != null)
            return BuildErrorResult(operationError);

        var payload = new Payload<TData>(data);
        return new JsonResult(payload);
    }

    private static IActionResult BuildErrorResult(OperationError operationError)
    {
        var message = operationError.Message ?? operationError.Code;

        // Message is the status description.
        return new HttpStatusCodeResult(400, message);
    }
}
