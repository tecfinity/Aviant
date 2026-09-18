// ReSharper disable MemberCanBePrivate.Global

using Aviant.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;
using Aviant.Application.Persistence.Orchestration;

#pragma warning disable 8618
namespace Aviant.Application.Persistence.UseCases;

/// <inheritdoc />
/// <summary>
///     UseCase abstract class with input, output and DbContext
/// </summary>
/// <typeparam name="TUseCaseInput">The expected input type</typeparam>
/// <typeparam name="TUseCaseOutput">The expected output type</typeparam>
/// <typeparam name="TDbContext">The expected DbContext type</typeparam>
public abstract class UseCase<TUseCaseInput, TUseCaseOutput, TDbContext>
    : UseCase<TUseCaseInput, TUseCaseOutput>
    where TUseCaseInput : class, IUseCaseInput
    where TUseCaseOutput : class, IUseCaseOutput
    where TDbContext : IDbContextWrite
{
    /// <summary>
    ///     The orchestrator object
    /// </summary>
    protected new IOrchestrator<TDbContext> Orchestrator =>
        Services.GetRequiredService<IOrchestrator<TDbContext>>();
}
