using System.Text.Json;
using Aviant.Application.Exceptions;
using Aviant.Core.Exceptions;
using Aviant.Presentation.AspNetCore;
using AwesomeAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aviant.Tests.AspNetCore.Unit;

public sealed class ExceptionHandlerTests
{
    [Fact]
    public async Task AValidationFailureIsA400ListingTheErrors()
    {
        var (status, body) = await HandleAsync(new ValidationException([new ValidationFailure("Title", "Title is required.")]));

        status.Should().Be(400);
        body.GetProperty("title").GetString().Should().Be("One or more validation failures have occurred.");
        body.GetProperty("errors").GetProperty("Title")[0].GetString().Should().Be("Title is required.");
    }

    [Fact]
    public async Task AMissingResourceIsA404()
    {
        var (status, body) = await HandleAsync(new NotFoundException("TodoList", 42));

        status.Should().Be(404);
        body.GetProperty("detail").GetString().Should().Contain("42");
    }

    [Fact]
    public async Task AMissingEntityIsA404Too()
    {
        var (status, _) = await HandleAsync(new EntityNotFoundException(typeof(object), 7));

        status.Should().Be(404);
    }

    [Fact]
    public async Task ARefusedDomainRuleIsA400WithItsMessage()
    {
        // The same status the orchestrator gives a refusal it returns as a failed response.
        var (status, body) = await HandleAsync(new DomainRuleException("An archived list cannot take new items."));

        status.Should().Be(400);
        body.GetProperty("detail").GetString().Should().Be("An archived list cannot take new items.");
    }

    [Fact]
    public async Task AnyOtherExceptionIsLeftToTheNextHandler()
    {
        var handled = await TryHandleAsync(new InvalidOperationException("boom"));

        handled.Should().BeFalse("unmapped exceptions fall through to the default 500 problem details");
    }

    [Fact]
    public async Task AnApplicationCanMapItsOwnExceptions()
    {
        var (status, body) = await HandleAsync(
            new TimeoutException("The payment provider did not answer."),
            options => options.Map<TimeoutException>(StatusCodes.Status503ServiceUnavailable, "A dependency is unavailable."));

        status.Should().Be(503);
        body.GetProperty("title").GetString().Should().Be("A dependency is unavailable.");
    }

    private static async Task<(int Status, JsonElement Body)> HandleAsync(
        Exception                         exception,
        Action<AviantProblemDetailsOptions>? configure = null)
    {
        var context = await RunAsync(exception, configure);
        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body, cancellationToken: TestContext.Current.CancellationToken);

        return (context.Response.StatusCode, json.RootElement.Clone());
    }

    private static async Task<bool> TryHandleAsync(Exception exception) =>
        (await RunAsync(exception, null)).Items.ContainsKey("handled");

    private static async Task<HttpContext> RunAsync(Exception exception, Action<AviantProblemDetailsOptions>? configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new Environment());
        services.AddAviantProblemDetails(configure);
        await using var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = provider };
        context.Response.Body = new MemoryStream();

        foreach (var handler in provider.GetServices<IExceptionHandler>())
            if (await handler.TryHandleAsync(context, exception, TestContext.Current.CancellationToken))
            {
                context.Items["handled"] = true;
                break;
            }

        return context;
    }

    private sealed class Environment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "Tests";

        public string ContentRootPath { get; set; } = string.Empty;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
