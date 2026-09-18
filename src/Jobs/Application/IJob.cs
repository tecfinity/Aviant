namespace Aviant.Application.Jobs;

/// <summary>
///     A background job that takes options. Enqueue it with <see cref="IJobRunner" />.
/// </summary>
/// <typeparam name="TJobOptions">The job's arguments, serialised with the job.</typeparam>
public interface IJob<in TJobOptions>
    where TJobOptions : class, IJobOptions
{
    /// <summary>
    ///     Runs the job.
    /// </summary>
    /// <param name="jobOptions">The arguments given when the job was enqueued.</param>
    /// <param name="cancellationToken">Cancelled when the job server shuts down or the job is deleted.</param>
    Task PerformAsync(TJobOptions jobOptions, CancellationToken cancellationToken);
}
