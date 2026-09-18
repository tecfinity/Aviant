using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aviant.Infrastructure.Jobs;

/// <summary>
///     Resolves every job once at startup and reports those that cannot be constructed.
/// </summary>
internal sealed partial class JobConstructionCheck(
    IServiceProvider               services,
    IReadOnlyCollection<Type>      jobTypes,
    bool                           failOnUnresolvable,
    ILogger<JobConstructionCheck> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        List<string> broken = [];

        foreach (var jobType in jobTypes)
        {
            using var scope = services.CreateScope();

            try
            {
                scope.ServiceProvider.GetRequiredService(jobType);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogUnresolvable(logger, exception, jobType.Name);
                broken.Add($"{jobType.FullName}: {exception.Message}");
            }
        }

        if (failOnUnresolvable && broken.Count > 0)
            throw new InvalidOperationException(
                "These jobs cannot be constructed and would fail every time they run:"
              + Environment.NewLine
              + string.Join(Environment.NewLine, broken));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Error, Message = "Job {Job} cannot be constructed and will fail every time it runs")]
    private static partial void LogUnresolvable(ILogger logger, Exception exception, string job);
}
