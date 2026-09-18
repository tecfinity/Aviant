using Aviant.Application.Jobs;
using Aviant.Infrastructure.Jobs;
using AwesomeAssertions;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aviant.Tests.Jobs.Unit;

public sealed class JobRegistrationTests
{
    [Fact]
    public void JobsInTheGivenAssembliesAreRegisteredWithTheRunner()
    {
        var services = new ServiceCollection();

        services.AddAviantJobs(jobs => jobs.AddAssemblies(typeof(JobRegistrationTests).Assembly));

        services.Should().Contain(d => d.ServiceType == typeof(IJobRunner));
        services.Should().Contain(d => d.ServiceType == typeof(ResolvableJob));
        services.Should().Contain(d => d.ServiceType == typeof(JobWithAMissingDependency));
    }

    [Fact]
    public async Task AJobThatCannotBeBuiltIsReportedAtStartupWithoutStoppingTheHost()
    {
        var sink     = new List<string>();
        var services = BaseServices(sink);
        services.AddAviantJobs(jobs => jobs.AddAssemblies(typeof(JobRegistrationTests).Assembly));
        await using var provider = services.BuildServiceProvider();

        foreach (var hosted in provider.GetServices<IHostedService>())
            await hosted.StartAsync(TestContext.Current.CancellationToken);

        sink.Should().ContainSingle(message => message.Contains(nameof(JobWithAMissingDependency), StringComparison.Ordinal))
           .Which.Should().Contain(nameof(IMissingDependency));
        sink.Should().NotContain(message => message.Contains(nameof(ResolvableJob), StringComparison.Ordinal));
    }

    [Fact]
    public async Task StartupCanBeMadeToFailOnAnUnbuildableJob()
    {
        var services = BaseServices([]);
        services.AddAviantJobs(jobs =>
        {
            jobs.AddAssemblies(typeof(JobRegistrationTests).Assembly);
            jobs.FailOnUnresolvableJobs = true;
        });
        await using var provider = services.BuildServiceProvider();

        var act = async () =>
        {
            foreach (var hosted in provider.GetServices<IHostedService>())
                await hosted.StartAsync(TestContext.Current.CancellationToken);
        };

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain(nameof(JobWithAMissingDependency));
    }

    [Fact]
    public async Task AnInterfaceJobIsCheckedToo()
    {
        var sink     = new List<string>();
        var services = BaseServices(sink);
        services.AddAviantJobs(jobs => jobs.Validate(typeof(IUnregisteredSweep)));
        await using var provider = services.BuildServiceProvider();

        foreach (var hosted in provider.GetServices<IHostedService>())
            await hosted.StartAsync(TestContext.Current.CancellationToken);

        sink.Should().ContainSingle(message => message.Contains(nameof(IUnregisteredSweep), StringComparison.Ordinal));
    }

    [Fact]
    public async Task AJobWhoseDependencyThrowsWhileBeingBuiltIsReportedToo()
    {
        var sink     = new List<string>();
        var services = BaseServices(sink);
        services.AddScoped<IMissingDependency>(_ => throw new ArgumentNullException("value", "a setting is missing"));
        services.AddAviantJobs(jobs => jobs.Validate(typeof(JobWithAMissingDependency)));
        services.AddTransient<JobWithAMissingDependency>();
        await using var provider = services.BuildServiceProvider();

        var act = async () =>
        {
            foreach (var hosted in provider.GetServices<IHostedService>())
                await hosted.StartAsync(TestContext.Current.CancellationToken);
        };

        await act.Should().NotThrowAsync("a job that cannot be built is reported, not fatal");
        sink.Should().ContainSingle(message => message.Contains("a setting is missing", StringComparison.Ordinal));
    }

    private static ServiceCollection BaseServices(List<string> sink)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddProvider(new SinkProvider(sink)));
        services.AddSingleton<IBackgroundJobClient>(_ => throw new InvalidOperationException("not used"));
        services.AddSingleton<IRecurringJobManager>(_ => throw new InvalidOperationException("not used"));

        return services;
    }

    public interface IMissingDependency;

    public interface IUnregisteredSweep : IRecurringJob;

    public sealed class NoOptions : IJobOptions;

    public sealed class ResolvableJob : IJob<NoOptions>
    {
        public Task PerformAsync(NoOptions jobOptions, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public sealed class JobWithAMissingDependency(IMissingDependency dependency) : IJob<NoOptions>
    {
        public Task PerformAsync(NoOptions jobOptions, CancellationToken cancellationToken) =>
            Task.FromResult(dependency);
    }

    private sealed class SinkProvider(List<string> sink) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new Logger(sink);

        public void Dispose()
        { }

        private sealed class Logger(List<string> sink) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (IsEnabled(logLevel))
                    sink.Add(formatter(state, exception) + " " + exception?.Message);
            }
        }
    }
}
