using Aviant.Application.ApplicationEvents;
using Aviant.Application.Commands;
using Aviant.Application.Orchestration;
using Aviant.Application.Queries;
using Aviant.Core.Exceptions;
using Aviant.Core.Messages;
using AwesomeAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aviant.Tests.Kernel.Unit.Orchestration;

public sealed class OrchestratorRefusalTests
{
    [Fact]
    public async Task DomainRuleExceptionBecomesAFailedResponseCarryingItsMessage()
    {
        var orchestrator = Build(new DomainRuleException("Cannot record a result while the event is Draft"));

        var response = await orchestrator.SendCommandAsync(new Act(), TestContext.Current.CancellationToken);

        response.Succeeded.Should().BeFalse();
        response.Messages.Should().ContainSingle().Which.Should().Be("Cannot record a result while the event is Draft");
    }

    [Fact]
    public async Task SubclassOfDomainRuleExceptionIsAlsoARefusal()
    {
        var orchestrator = Build(new FighterAlreadyBooked());

        var response = await orchestrator.SendCommandAsync(new Act(), TestContext.Current.CancellationToken);

        response.Succeeded.Should().BeFalse();
        response.Messages.Should().ContainSingle().Which.Should().Be("Fighter is already booked on this card");
    }

    [Fact]
    public async Task ExceptionTypeRegisteredAsRefusalBecomesAFailedResponse()
    {
        var orchestrator = Build(
            new InvalidOperationException("Already published"),
            options => options.TreatAsRefusal<InvalidOperationException>());

        var response = await orchestrator.SendCommandAsync(new Act(), TestContext.Current.CancellationToken);

        response.Succeeded.Should().BeFalse();
        response.Messages.Should().ContainSingle().Which.Should().Be("Already published");
    }

    [Fact]
    public async Task ExceptionThatIsNotARefusalPropagates()
    {
        var orchestrator = Build(new InvalidOperationException("Connection pool exhausted"));

        var act = () => orchestrator.SendCommandAsync(new Act(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Connection pool exhausted");
    }

    [Fact]
    public async Task QueryRefusalBecomesAFailedResponse()
    {
        var orchestrator = Build(new DomainRuleException("Results are hidden until the event is final"));

        var response = await orchestrator.SendQueryAsync(new Look(), TestContext.Current.CancellationToken);

        response.Succeeded.Should().BeFalse();
        response.Messages.Should().ContainSingle().Which.Should().Be("Results are hidden until the event is final");
    }

    [Fact]
    public async Task SuccessfulCommandStillReturnsItsPayload()
    {
        var orchestrator = Build(throwing: null);

        var response = await orchestrator.SendCommandAsync(new Act(), TestContext.Current.CancellationToken);

        response.Succeeded.Should().BeTrue();
        response.Payload<string>().Should().Be("done");
    }

    private static IOrchestrator Build(Exception? throwing, Action<OrchestratorOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Thrower(throwing));
        services.AddTransient<IMediator, Mediator>();
        services.AddTransient<IRequestHandler<Act, string>, ActHandler>();
        services.AddTransient<IRequestHandler<Look, string>, LookHandler>();
        services.AddScoped<IMessages, Messages>();
        services.AddScoped<IApplicationEventDispatcher, ApplicationEventDispatcher>();
        services.AddOptions<OrchestratorOptions>().Configure(options => configure?.Invoke(options));
        services.AddScoped<IOrchestrator, Orchestrator>();

        return services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<IOrchestrator>();
    }

    private sealed record Thrower(Exception? Exception)
    {
        public string Run() => Exception is null ? "done" : throw Exception;
    }

    private sealed record Act : Command<string>;

    private sealed record Look : Query<string>;

    private sealed class ActHandler(Thrower thrower) : CommandHandler<Act, string>
    {
        public override Task<string> Handle(Act command, CancellationToken cancellationToken) =>
            Task.FromResult(thrower.Run());
    }

    private sealed class LookHandler(Thrower thrower) : QueryHandler<Look, string>
    {
        public override Task<string> Handle(Look request, CancellationToken cancellationToken) =>
            Task.FromResult(thrower.Run());
    }

    private sealed class FighterAlreadyBooked()
        : DomainRuleException("Fighter is already booked on this card");
}
