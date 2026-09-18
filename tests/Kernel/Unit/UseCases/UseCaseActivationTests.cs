using Aviant.Application.ApplicationEvents;
using Aviant.Application.Commands;
using Aviant.Application.Extensions;
using Aviant.Application.Orchestration;
using Aviant.Application.UseCases;
using Aviant.Core.Messages;
using AwesomeAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ValidationException = Aviant.Application.Exceptions.ValidationException;

namespace Aviant.Tests.Kernel.Unit.UseCases;

public sealed class UseCaseActivationTests
{
    [Fact]
    public async Task AUseCaseFromAnyDependencyScopeReachesItsOrchestrator()
    {
        // No HttpContext anywhere: the same resolution a background job or worker performs.
        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ShoutUseCase>();
        var output  = new Output();
        useCase.SetOutput(output);

        await useCase.ExecuteAsync(new ShoutInput("hello"), TestContext.Current.CancellationToken);

        output.Result.Should().Be("HELLO");
    }

    [Fact]
    public async Task InputValidatorsComeFromTheSameScope()
    {
        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ShoutUseCase>();
        useCase.SetOutput(new Output());

        var act = () => useCase.ExecuteAsync(new ShoutInput(""), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AUseCaseThatWasNeverActivatedSaysHowToFixIt()
    {
        var useCase = new ShoutUseCase();
        useCase.SetOutput(new Output());

        var act = () => useCase.ExecuteAsync(new ShoutInput("hello"), TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<InvalidOperationException>())
           .Which.Message.Should().Contain(nameof(ShoutUseCase)).And.Contain("AddAviantUseCases");
    }

    [Fact]
    public void AnExplicitRegistrationIsReplacedByTheActivatingOne()
    {
        var services = new ServiceCollection();
        services.AddScoped<ShoutUseCase>();

        services.AddAviantUseCases([typeof(UseCaseActivationTests).Assembly]);

        services.Where(descriptor => descriptor.ServiceType == typeof(ShoutUseCase))
           .Should().ContainSingle().Which.ImplementationFactory.Should().NotBeNull();
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IMessages, Messages>();
        services.AddScoped<IApplicationEventDispatcher, ApplicationEventDispatcher>();
        services.AddScoped<IOrchestrator, Orchestrator>();
        services.AddTransient<IValidator<ShoutInput>, ShoutInputValidator>();
        services.AddAviantCqrs([typeof(UseCaseActivationTests).Assembly]);
        services.AddAviantUseCases([typeof(UseCaseActivationTests).Assembly]);

        return services.BuildServiceProvider();
    }

    public sealed record ShoutInput(string Text) : UseCaseInput;

    public sealed class ShoutInputValidator : AbstractValidator<ShoutInput>
    {
        public ShoutInputValidator() => RuleFor(input => input.Text).NotEmpty();
    }

    public sealed record Shout(string Text) : Command<string>;

    public sealed class ShoutHandler : CommandHandler<Shout, string>
    {
        public override Task<string> Handle(Shout command, CancellationToken cancellationToken) =>
            Task.FromResult(command.Text.ToUpperInvariant());
    }

    public interface IShoutOutput : IUseCaseOutput
    {
        public void Shouted(string text);
    }

    public sealed class ShoutUseCase : UseCase<ShoutInput, IShoutOutput>
    {
        public override async Task ExecuteAsync(ShoutInput input, CancellationToken cancellationToken = default)
        {
            await ValidateInputAsync(input, cancellationToken).ConfigureAwait(false);

            var response = await Orchestrator.SendCommandAsync(new Shout(input.Text), cancellationToken)
               .ConfigureAwait(false);

            Output.Shouted(response.Payload<string>());
        }
    }

    private sealed class Output : IShoutOutput
    {
        public string? Result { get; private set; }

        public void Shouted(string text) => Result = text;

        public void BadRequest(object? @object) => throw new InvalidOperationException(@object?.ToString());
    }
}
