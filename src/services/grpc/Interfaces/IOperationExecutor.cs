namespace Roblox.Web.Framework.Services.Grpc;

using System;
using System.Threading;
using System.Threading.Tasks;

using Operations;

/// <summary>
/// Executes operation interfaces.
/// </summary>
public interface IOperationExecutor
{
    /// <summary>
    /// Executes an <see cref="IOperation{TInput}"/>.
    /// </summary>
    /// <param name="action">The <see cref="IOperation{TInput}"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="action"/>
    /// </exception>
    void Execute(IOperation action);

    /// <summary>
    /// Executes an <see cref="IOperation{TInput}"/>.
    /// </summary>
    /// <typeparam name="TInput">The action input data type.</typeparam>
    /// <param name="action">The <see cref="IOperation{TInput}"/>.</param>
    /// <param name="input">The action input.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="action"/>
    /// </exception>
    void Execute<TInput>(IOperation<TInput> action, TInput input);

    /// <summary>
    /// Executes an <see cref="IResultOperation{TOutput}"/>.
    /// </summary>
    /// <param name="operation">The <see cref="IResultOperation{TOutput}"/>.</param>
    /// <returns>The <typeparamref name="TOutput"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="operation"/>
    /// </exception>
    TOutput Execute<TOutput>(IResultOperation<TOutput> operation);

    /// <summary>
    /// Executes an <see cref="IResultOperation{TOutput}"/>.
    /// </summary>
    /// <typeparam name="TInput">The operation input data type.</typeparam>
    /// <typeparam name="TOutput">The operation output data type.</typeparam>
    /// <param name="operation">The <see cref="IResultOperation{TOutput}"/>.</param>
    /// <param name="input">The operation input.</param>
    /// <returns>The <typeparamref name="TOutput"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="operation"/>
    /// </exception>
    TOutput Execute<TInput, TOutput>(IResultOperation<TInput, TOutput> operation, TInput input);

    /// <summary>
    /// Executes an <see cref="IAsyncOperation{TInput}"/>.
    /// </summary>
    /// <param name="action">The <see cref="IAsyncOperation{TInput}"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="action"/>
    /// </exception>
    Task ExecuteAsync(IAsyncOperation action, CancellationToken cancellationToken);

    /// <summary>
    /// Executes an <see cref="IAsyncOperation{TInput}"/>.
    /// </summary>
    /// <typeparam name="TInput">The action input data type.</typeparam>
    /// <param name="action">The <see cref="IAsyncOperation{TInput}"/>.</param>
    /// <param name="input">The action input.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="action"/>
    /// </exception>
    Task ExecuteAsync<TInput>(IAsyncOperation<TInput> action, TInput input, CancellationToken cancellationToken);

    /// <summary>
    /// Executes an <see cref="IAsyncResultOperation{TOutput}"/>.
    /// </summary>
    /// <param name="operation">The <see cref="IAsyncResultOperation{TOutput}"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>The <typeparamref name="TOutput"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="operation"/>
    /// </exception>
    Task<TOutput> ExecuteAsync<TOutput>(IAsyncResultOperation<TOutput> operation, CancellationToken cancellationToken);

    /// <summary>
    /// Executes an <see cref="IAsyncResultOperation{TOutput}"/>.
    /// </summary>
    /// <typeparam name="TInput">The operation input data type.</typeparam>
    /// <typeparam name="TOutput">The operation output data type.</typeparam>
    /// <param name="operation">The <see cref="IAsyncResultOperation{TOutput}"/>.</param>
    /// <param name="input">The operation input.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>The <typeparamref name="TOutput"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="operation"/>
    /// </exception>
    Task<TOutput> ExecuteAsync<TInput, TOutput>(IAsyncResultOperation<TInput, TOutput> operation, TInput input, CancellationToken cancellationToken);
}
