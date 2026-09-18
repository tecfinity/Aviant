using Aviant.Application.Commands;
using Aviant.Application.Extensions;
using Aviant.Application.Orchestration;
using AwesomeAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;
using ValidationException = Aviant.Application.Exceptions.ValidationException;

namespace Aviant.Tests.Kernel.Unit.Extensions;

public sealed class AddAviantCqrsTests
{
    [Fact]
    public async Task HandlersInTheGivenAssembliesAreRegisteredAndReachable()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<IMediator>()
           .Send(new Greet("Ada"), TestContext.Current.CancellationToken);

        result.Should().Be("Hello, Ada");
    }

    [Fact]
    public async Task ValidatorsRunInThePipeline()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var act = () => scope.ServiceProvider.GetRequiredService<IMediator>()
           .Send(new Greet(""), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public void OrchestratorOptionsAreConfigured()
    {
        using var provider = BuildProvider(options => options.TreatAsRefusal<InvalidOperationException>());

        var options = provider.GetRequiredService<IOptions<OrchestratorOptions>>().Value;

        options.IsRefusal(new InvalidOperationException()).Should().BeTrue();
    }

    [Fact]
    public async Task StartupFailsNamingEveryRequestThatHasNoHandler()
    {
        await using var provider = BuildProvider();
        var validator = provider.GetServices<IHostedService>().OfType<CqrsHandlerValidator>().Single();

        var act = () => validator.StartAsync(TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<InvalidOperationException>())
           .Which.Message.Should().Contain(nameof(Orphan)).And.NotContain(nameof(Greet));
    }

    [Fact]
    public async Task StartupSucceedsWhenEveryRequestHasAHandler()
    {
        var services = new ServiceCollection();
        services.AddAviantCqrs([typeof(ICommand).Assembly]);
        await using var provider = services.BuildServiceProvider();
        var validator = new CqrsHandlerValidator(
            [typeof(ICommand).Assembly],
            provider.GetRequiredService<IServiceProviderIsService>());

        var act = () => validator.StartAsync(TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartupFailsForRequestsInAReferencedModuleThatWasNeverRegistered()
    {
        // Nothing from this assembly is registered, but the host (root) references it.
        var services = new ServiceCollection();
        services.AddAviantCqrs([typeof(ICommand).Assembly]);
        await using var provider = services.BuildServiceProvider();
        var validator = new CqrsHandlerValidator(
            [],
            provider.GetRequiredService<IServiceProviderIsService>(),
            rootAssembly: typeof(AddAviantCqrsTests).Assembly);

        var act = () => validator.StartAsync(TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<InvalidOperationException>())
           .Which.Message.Should().Contain(nameof(Orphan)).And.Contain(nameof(Greet));
    }

    private static ServiceProvider BuildProvider(Action<OrchestratorOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidatorsFromAssemblyContaining<AddAviantCqrsTests>(includeInternalTypes: true);
        services.AddAviantCqrs([typeof(AddAviantCqrsTests).Assembly], configure);

        return services.BuildServiceProvider();
    }

    public sealed record Greet(string Name) : Command<string>;

    public sealed class GreetHandler : CommandHandler<Greet, string>
    {
        public override Task<string> Handle(Greet command, CancellationToken cancellationToken) =>
            Task.FromResult($"Hello, {command.Name}");
    }

    public sealed class GreetValidator : AbstractValidator<Greet>
    {
        public GreetValidator() => RuleFor(greet => greet.Name).NotEmpty();
    }

    /// <summary>A request nobody handles, which the startup validation must report.</summary>
    public sealed record Orphan : Command<string>;
}
