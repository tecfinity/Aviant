// ReSharper disable MemberCanBePrivate.Global

using Aviant.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;
using Aviant.Core.EventSourcing.Aggregates;
using Aviant.Application.EventSourcing.Orchestration;

#pragma warning disable 8618
namespace Aviant.Application.EventSourcing.UseCases;



/// <inheritdoc />
/// <summary>
///     UseCase abstract class with input, output, aggregate and aggregate id
/// </summary>
/// <typeparam name="TUseCaseInput">The expected input type</typeparam>
/// <typeparam name="TUseCaseOutput">The expected output type</typeparam>
/// <typeparam name="TAggregate">The expected aggregate type</typeparam>
/// <typeparam name="TAggregateId">The expected aggregate id type</typeparam>
public abstract class UseCase<TUseCaseInput, TUseCaseOutput, TAggregate, TAggregateId>
    : UseCase<TUseCaseInput, TUseCaseOutput>
    where TUseCaseInput : class, IUseCaseInput
    where TUseCaseOutput : class, IUseCaseOutput
    where TAggregate : class, IAggregate<TAggregateId>
    where TAggregateId : class, IAggregateId
{
    /// <summary>
    ///     The orchestrator object
    /// </summary>
    protected new IOrchestrator<TAggregate, TAggregateId> Orchestrator =>
        Services.GetRequiredService<IOrchestrator<TAggregate, TAggregateId>>();
}
