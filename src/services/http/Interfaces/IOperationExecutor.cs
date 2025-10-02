namespace Roblox.Web.Framework.Services.Http;

using System;
using System.Threading;
using System.Threading.Tasks;

using IActionResult = Microsoft.AspNetCore.Mvc.IActionResult;

using Operations;

/// <summary>
/// Executes operation interfaces and translates them to <see cref="IActionResult"/>s.
/// </summary>
public interface IOperationExecutor
{
    /// <summary>
    /// Executes an <see cref="IOperation{TInput}"/> and converts the result to an <see cref="IActionResult"/>.
    /// </summary>
    /// <param name="action">The <see cref="IOperation{TInput}"/>.</param>
    /// <returns>The <see cref="IActionResult"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="action"/>
    /// </exception>
    IActionResult Execute(IOperation action);

    /// <summary>
    /// Executes an <see cref="IOperation{TInput}"/> and converts the result to an <see cref="IActionResult"/>.
    /// </summary>
    /// <typeparam name="TInput">The action input data type.</typeparam>
    /// <param name="action">The <see cref="IOperation{TInput}"/>.</param>
    /// <param name="input">The action input.</param>
    /// <returns>The <see cref="IActionResult"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="action"/>
    /// </exception>
    IActionResult Execute<TInput>(IOperation<TInput> action, TInput input);

    /// <summary>
    /// Executes an <see cref="IResultOperation{TOutput}"/> and converts the result to an <see cref="IActionResult"/>.
    /// </summary>
    /// <param name="operation">The <see cref="IResultOperation{TOutput}"/>.</param>
    /// <returns>The <see cref="IActionResult"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="operation"/>
    /// </exception>
    IActionResult Execute<TOutput>(IResultOperation<TOutput> operation);

    /// <summary>
    /// Executes an <see cref="IResultOperation{TOutput}"/> and converts the result to an <see cref="IActionResult"/>.
    /// </summary>
    /// <typeparam name="TInput">The operation input data type.</typeparam>
    /// <typeparam name="TOutput">The operation output data type.</typeparam>
    /// <param name="operation">The <see cref="IResultOperation{TOutput}"/>.</param>
    /// <param name="input">The operation input.</param>
    /// <returns>The <see cref="IActionResult"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="operation"/>
    /// </exception>
    IActionResult Execute<TInput, TOutput>(IResultOperation<TInput, TOutput> operation, TInput input);

    /// <summary>
    /// Executes an <see cref="IAsyncOperation{TInput}"/> and converts the result to an <see cref="IActionResult"/>.
    /// </summary>
    /// <param name="action">The <see cref="IAsyncOperation{TInput}"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="IActionResult"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="action"/>
    /// </exception>
    Task<IActionResult> ExecuteAsync(IAsyncOperation action, CancellationToken cancellationToken);

    /// <summary>
    /// Executes an <see cref="IAsyncOperation{TInput}"/> and converts the result to an <see cref="IActionResult"/>.
    /// </summary>
    /// <typeparam name="TInput">The action input data type.</typeparam>
    /// <param name="action">The <see cref="IAsyncOperation{TInput}"/>.</param>
    /// <param name="input">The action input.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="IActionResult"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="action"/>
    /// </exception>
    Task<IActionResult> ExecuteAsync<TInput>(IAsyncOperation<TInput> action, TInput input, CancellationToken cancellationToken);

    /// <summary>
    /// Executes an <see cref="IAsyncResultOperation{TOutput}"/> and converts the result to an <see cref="IActionResult"/>.
    /// </summary>
    /// <param name="operation">The <see cref="IAsyncResultOperation{TOutput}"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="IActionResult"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="operation"/>
    /// </exception>
    Task<IActionResult> ExecuteAsync<TOutput>(IAsyncResultOperation<TOutput> operation, CancellationToken cancellationToken);

    /// <summary>
    /// Executes an <see cref="IAsyncResultOperation{TOutput}"/> and converts the result to an <see cref="IActionResult"/>.
    /// </summary>
    /// <typeparam name="TInput">The operation input data type.</typeparam>
    /// <typeparam name="TOutput">The operation output data type.</typeparam>
    /// <param name="operation">The <see cref="IAsyncResultOperation{TOutput}"/>.</param>
    /// <param name="input">The operation input.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="IActionResult"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="operation"/>
    /// </exception>
    Task<IActionResult> ExecuteAsync<TInput, TOutput>(IAsyncResultOperation<TInput, TOutput> operation, TInput input, CancellationToken cancellationToken);
}
