using Aviant.Application.Commands;
using Aviant.Application.Extensions;
using AwesomeAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aviant.Tests.Kernel.Unit.Behaviours;

public sealed class PipelineLoggingTests
{
    [Fact]
    public async Task AFailingRequestIsLoggedThroughTheApplicationsLogger()
    {
        var sink = new LogSink();
        await using var provider = BuildProvider(sink);
        await using var scope = provider.CreateAsyncScope();

        var act = () => scope.ServiceProvider.GetRequiredService<IMediator>()
           .Send(new SignIn("ada", "correct horse battery staple"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
        sink.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Error
         && entry.Exception is InvalidOperationException
         && entry.Message.Contains(nameof(SignIn), StringComparison.Ordinal));
    }

    [Fact]
    public async Task RequestContentsAreNeverWrittenToTheLog()
    {
        var sink = new LogSink();
        await using var provider = BuildProvider(sink);
        await using var scope = provider.CreateAsyncScope();

        try
        {
            await scope.ServiceProvider.GetRequiredService<IMediator>()
               .Send(new SignIn("ada", "correct horse battery staple"), TestContext.Current.CancellationToken);
        }
        catch (InvalidOperationException)
        { }

        sink.Entries.Should().NotBeEmpty();
        sink.Entries.Should().NotContain(entry => entry.Message.Contains("correct horse", StringComparison.Ordinal));
    }

    private static ServiceProvider BuildProvider(LogSink sink)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Trace).AddProvider(sink));
        services.AddAviantCqrs([typeof(PipelineLoggingTests).Assembly]);

        return services.BuildServiceProvider();
    }

    public sealed record SignIn(string UserName, string Password) : Command<string>;

    public sealed class SignInHandler : CommandHandler<SignIn, string>
    {
        public override Task<string> Handle(SignIn command, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The directory is unreachable.");
    }

    private sealed record Entry(LogLevel Level, string Message, Exception? Exception);

    private sealed class LogSink : ILoggerProvider
    {
        private readonly List<Entry> _entries = [];

        public IReadOnlyList<Entry> Entries
        {
            get
            {
                lock (_entries)
                    return [.. _entries];
            }
        }

        public ILogger CreateLogger(string categoryName) => new Logger(this);

        public void Dispose()
        { }

        private sealed class Logger(LogSink sink) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                lock (sink._entries)
                    sink._entries.Add(new Entry(logLevel, formatter(state, exception), exception));
            }
        }
    }
}
