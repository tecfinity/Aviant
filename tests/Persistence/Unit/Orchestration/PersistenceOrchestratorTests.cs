using Aviant.Application.ApplicationEvents;
using Aviant.Application.Commands;
using Aviant.Application.Orchestration;
using Aviant.Application.Persistence;
using Aviant.Application.Persistence.Orchestration;
using Aviant.Core.Exceptions;
using Aviant.Core.Messages;
using AwesomeAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aviant.Tests.Persistence.Unit.Orchestration;

public sealed class PersistenceOrchestratorTests
{
    [Fact]
    public async Task SuccessfulCommitReturnsThePayload()
    {
        var orchestrator = Build(handlerThrows: null, commitThrows: null);

        var response = await orchestrator.SendCommandAsync(new Act(), TestContext.Current.CancellationToken);

        response.Succeeded.Should().BeTrue();
        response.Payload<string>().Should().Be("done");
    }

    [Fact]
    public async Task HandlerRefusalBecomesAFailedResponseAndNothingIsCommitted()
    {
        var unitOfWork   = new FakeUnitOfWork(null);
        var orchestrator = Build(new DomainRuleException("Slug already taken"), null, unitOfWork);

        var response = await orchestrator.SendCommandAsync(new Act(), TestContext.Current.CancellationToken);

        response.Succeeded.Should().BeFalse();
        response.Messages.Should().ContainSingle().Which.Should().Be("Slug already taken");
        unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task ConcurrencyConflictAtCommitBecomesAFailedResponse()
    {
        var orchestrator = Build(null, new DbUpdateConcurrencyException("The record was changed by someone else"));

        var response = await orchestrator.SendCommandAsync(new Act(), TestContext.Current.CancellationToken);

        response.Succeeded.Should().BeFalse();
        response.Messages.Should().ContainSingle().Which.Should().Be("The record was changed by someone else");
    }

    [Fact]
    public async Task RefusalRaisedWhileCommittingBecomesAFailedResponse()
    {
        var orchestrator = Build(null, new DomainRuleException("Project scope cannot change"));

        var response = await orchestrator.SendCommandAsync(new Act(), TestContext.Current.CancellationToken);

        response.Succeeded.Should().BeFalse();
        response.Messages.Should().ContainSingle().Which.Should().Be("Project scope cannot change");
    }

    [Fact]
    public async Task UnexpectedFailureAtCommitPropagatesInsteadOfBecomingAMessage()
    {
        var orchestrator = Build(null, new NullReferenceException("bug"));

        var act = () => orchestrator.SendCommandAsync(new Act(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NullReferenceException>();
    }

    private static IOrchestrator<FakeDbContext> Build(
        Exception?      handlerThrows,
        Exception?      commitThrows,
        FakeUnitOfWork? unitOfWork = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Thrower(handlerThrows));
        services.AddTransient<IMediator, Mediator>();
        services.AddTransient<IRequestHandler<Act, string>, ActHandler>();
        services.AddScoped<IMessages, Messages>();
        services.AddScoped<IApplicationEventDispatcher, ApplicationEventDispatcher>();
        services.AddOptions<OrchestratorOptions>();
        services.AddSingleton<IUnitOfWork<FakeDbContext>>(unitOfWork ?? new FakeUnitOfWork(commitThrows));
        services.AddScoped<IOrchestrator<FakeDbContext>, Orchestrator<FakeDbContext>>();

        return services.BuildServiceProvider().CreateScope().ServiceProvider
           .GetRequiredService<IOrchestrator<FakeDbContext>>();
    }

    private sealed record Thrower(Exception? Exception)
    {
        public string Run() => Exception is null ? "done" : throw Exception;
    }

    private sealed record Act : Command<string>;

    private sealed class ActHandler(Thrower thrower) : CommandHandler<Act, string>
    {
        public override Task<string> Handle(Act command, CancellationToken cancellationToken) =>
            Task.FromResult(thrower.Run());
    }

    public sealed class FakeDbContext : IDbContextWrite
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public void Dispose()
        { }
    }

    private sealed class FakeUnitOfWork(Exception? commitThrows) : IUnitOfWork<FakeDbContext>
    {
        public int Commits { get; private set; }

        public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            Commits++;

            return commitThrows is null ? Task.FromResult(1) : throw commitThrows;
        }
    }
}
