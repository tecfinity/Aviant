namespace Aviant.Application.Jobs;

/// <summary>
///     A job that runs on a schedule and decides for itself what is due, so changing a setting takes effect on
///     the next run instead of the next deployment. Schedule it with
///     <see cref="IJobRunner.RunRecurring{TJob}(string, string, TimeZoneInfo?, string?)" />.
/// </summary>
public interface IRecurringJob
{
    /// <summary>
    ///     Runs one pass.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the job server shuts down.</param>
    Task RunAsync(CancellationToken cancellationToken);
}
