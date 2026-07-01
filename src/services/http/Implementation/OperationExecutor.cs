using System.Reflection;

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
    private static readonly HttpStatusCodeResult OkResult = new(200);
    
    /// <inheritdoc cref="IOperationExecutor.Execute(IOperation)"/>
    public IActionResult Execute(IOperation action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var error = action.Execute();
        return error != null ? BuildErrorResult(error) : OkResult;
    }

    /// <inheritdoc cref="IOperationExecutor.Execute{TInput}(IOperation{TInput}, TInput)"/>
    public IActionResult Execute<TInput>(IOperation<TInput> action, TInput input)
    {
        ArgumentNullException.ThrowIfNull(action);

        var error = action.Execute(input);
        return error != null ? BuildErrorResult(error) : OkResult;
    }

    /// <inheritdoc cref="IOperationExecutor.Execute{TOutput}(IResultOperation{TOutput})"/>
    public IActionResult Execute<TOutput>(IResultOperation<TOutput> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var (data, operationError) = operation.Execute();
        return BuildPayloadResult(data, operationError);
    }

    /// <inheritdoc cref="IOperationExecutor.Execute{TInput,TOutput}(IResultOperation{TInput,TOutput}, TInput)"/>
    public IActionResult Execute<TInput, TOutput>(IResultOperation<TInput, TOutput> operation, TInput input)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var (data, operationError) = operation.Execute(input);
        return BuildPayloadResult(data, operationError);
    }

    /// <inheritdoc cref="IOperationExecutor.ExecuteAsync(IAsyncOperation, CancellationToken)"/>
    public async Task<IActionResult> ExecuteAsync(IAsyncOperation action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        var error = await action.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        return error != null ? BuildErrorResult(error) : OkResult;
    }

    /// <inheritdoc cref="IOperationExecutor.ExecuteAsync{TInput}(IAsyncOperation{TInput}, TInput, CancellationToken)"/>
    public async Task<IActionResult> ExecuteAsync<TInput>(IAsyncOperation<TInput> action, TInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        var error = await action.ExecuteAsync(input, cancellationToken).ConfigureAwait(false);
        return error != null ? BuildErrorResult(error) : OkResult;
    }

    /// <inheritdoc cref="IOperationExecutor.ExecuteAsync{TOutput}(IAsyncResultOperation{TOutput}, CancellationToken)"/>
    public async Task<IActionResult> ExecuteAsync<TOutput>(IAsyncResultOperation<TOutput> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var (data, operationError) = await operation.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        return BuildPayloadResult(data, operationError);
    }

    /// <inheritdoc cref="IOperationExecutor.ExecuteAsync{TInput,TOutput}(IAsyncResultOperation{TInput,TOutput}, TInput, CancellationToken)"/>
    public async Task<IActionResult> ExecuteAsync<TInput, TOutput>(IAsyncResultOperation<TInput, TOutput> operation, TInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

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
    
    /// <summary>
    /// Builds a <see cref="ActionResult"/> from an <see cref="OperationError"/>.
    /// </summary>
    /// <remarks>
    /// Before this used to set the reason phrase of the response to the <see cref="OperationError.Message"/>. 
    /// This is doesn't make sense anymore for HTTP/2 and HTTP/3, so we just return the <see cref="OperationError"/> as the response body
    /// as it has Json attributes to ignore the <see cref="OperationError.Message"/> if it is null or empty.
    /// </remarks>
    private static ActionResult BuildErrorResult(OperationError operationError)
    {
        var withDetails = true;
        var statusCode = 400;

        if (operationError.Code != null)
        {
            var statusCodeAttribute = operationError.Code.GetType().GetField(operationError.Code.ToString())?.GetCustomAttribute<HttpStatusCodeAttribute>();
            if (statusCodeAttribute != null)
            {
                statusCode = statusCodeAttribute.StatusCode;
                withDetails = statusCodeAttribute.WithDetails;
            }
        }
        
        // Message is the status description.
        if (!withDetails)
            return new StatusCodeResult(statusCode);
        
        return new JsonResult(new { error = operationError }) { StatusCode = statusCode };
    }
}
