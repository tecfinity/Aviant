// ReSharper disable MemberCanBePrivate.Global

using Aviant.Application.Behaviours;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using Aviant.Application.Orchestration;

#pragma warning disable 8618
namespace Aviant.Application.UseCases;

/// <summary>
///     The Use Case Abstract Base class
/// </summary>
/// <typeparam name="TUseCaseOutput">The expected output object type</typeparam>
public abstract class UseCaseBase<TUseCaseOutput>
    : IUseCase<TUseCaseOutput>,
      IUseCaseActivation
    where TUseCaseOutput : class, IUseCaseOutput
{
    private IServiceProvider? _services;

    /// <summary>
    ///     The output object
    /// </summary>
    protected TUseCaseOutput Output;

    /// <summary>
    ///     The dependency scope this use case runs in: a request's, a background job's or a test's.
    /// </summary>
    protected IServiceProvider Services =>
        _services
     ?? throw new InvalidOperationException(
            $"{GetType().Name} was not activated. Register use cases with services.AddAviantUseCases(assemblies), "
          + "or call Activate(serviceProvider) when constructing one yourself.");

    /// <summary>
    ///     The orchestrator object
    /// </summary>
    protected IOrchestrator Orchestrator => Services.GetRequiredService<IOrchestrator>();

    #region IUseCase<TUseCaseOutput> Members

    /// <summary>
    ///     Sets the Output object
    /// </summary>
    /// <param name="output">The output object</param>
    public void SetOutput(TUseCaseOutput output) => Output = output;

    #endregion

    #region IUseCaseActivation Members

    /// <inheritdoc />
    public void Activate(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _services = services;
    }

    #endregion
}

/// <inheritdoc cref="Aviant.Application.UseCases.UseCaseBase{TUseCaseOutput}" />
/// <inheritdoc cref="Aviant.Application.UseCases.IUseCaseExecute" />
/// <summary>
///     UseCase abstract class (without input data)
/// </summary>
/// <typeparam name="TUseCaseOutput">The expected output object type</typeparam>
public abstract class UseCase<TUseCaseOutput>
    : UseCaseBase<TUseCaseOutput>,
      IUseCaseExecute
    where TUseCaseOutput : class, IUseCaseOutput
{
    #region IUseCaseExecute Members

    /// <summary>
    ///     Executes the current use case asynchronously
    /// </summary>
    /// <param name="cancellationToken">The cancellation token object</param>
    /// <returns></returns>
    public abstract Task ExecuteAsync(CancellationToken cancellationToken = default);

    #endregion
}

/// <inheritdoc cref="Aviant.Application.UseCases.UseCaseBase{TUseCaseOutput}" />
/// <inheritdoc cref="Aviant.Application.UseCases.IUseCaseExecute{TUseCaseInput}" />
/// <summary>
///     UseCase abstract class with input and output
/// </summary>
/// <typeparam name="TUseCaseInput"></typeparam>
/// <typeparam name="TUseCaseOutput"></typeparam>
public abstract class UseCase<TUseCaseInput, TUseCaseOutput>
    : UseCaseBase<TUseCaseOutput>,
      IUseCaseExecute<TUseCaseInput>
    where TUseCaseInput : class, IUseCaseInput
    where TUseCaseOutput : class, IUseCaseOutput
{
    #region IUseCaseExecute<TUseCaseInput> Members

    /// <summary>
    ///     Execute the current use case
    /// </summary>
    /// <param name="input">The input data object</param>
    /// <param name="cancellationToken">The cancellation token object</param>
    /// <returns></returns>
    public abstract Task ExecuteAsync(TUseCaseInput input, CancellationToken cancellationToken = default);

    #endregion

    /// <summary>
    ///     Run validation rules for current input object
    /// </summary>
    /// <param name="input">The input object</param>
    /// <param name="cancellationToken">The cancellation token object</param>
    /// <returns></returns>
    protected virtual Task ValidateInputAsync(
        TUseCaseInput     input,
        CancellationToken cancellationToken = default) =>
        new ValidationProcessor<TUseCaseInput>(Services.GetServices<IValidator<TUseCaseInput>>(), input)
           .HandleValidationAsync(cancellationToken);
}
