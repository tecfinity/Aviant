using Hangfire.States;

namespace Aviant.Application.Jobs;

public interface IJobRunner
{
    string Run<TJob, TJobOptions>(Action<TJobOptions>? configureJobOptions = null)
        where TJobOptions : class, IJobOptions
        where TJob : IJob<TJobOptions>;

    string RunInState<TJob, TJobOptions>(IState state, Action<TJobOptions>? configureJobOptions = null)
        where TJobOptions : class, IJobOptions
        where TJob : IJob<TJobOptions>;

    string RunWithDelay<TJob, TJobOptions>(TimeSpan delay, Action<TJobOptions>? configureJobOptions = null)
        where TJobOptions : class, IJobOptions
        where TJob : IJob<TJobOptions>;

    string RunAtDateTime<TJob, TJobOptions>(DateTimeOffset dateTime, Action<TJobOptions>? configureJobOptions = null)
        where TJobOptions : class, IJobOptions
        where TJob : IJob<TJobOptions>;

    string RunAfter<TJob, TJobOptions>(string previousJobId, Action<TJobOptions>? configureJobOptions = null)
        where TJobOptions : class, IJobOptions
        where TJob : IJob<TJobOptions>;

    string RunRecurring<TJob, TJobOptions>(
        string               jobId,
        string               cron,
        Action<TJobOptions>? configureJobOptions = null,
        TimeZoneInfo?        timeZone            = null,
        string?              queue               = null)
        where TJobOptions : class, IJobOptions
        where TJob : IJob<TJobOptions>;

    /// <summary>
    ///     Schedules <typeparamref name="TJob" /> on <paramref name="cron" />, replacing any schedule with the same id.
    /// </summary>
    /// <param name="jobId">A stable id; scheduling the same id again updates it.</param>
    /// <param name="cron">The schedule, e.g. <c>Cron.Hourly()</c> or <c>"*/15 * * * *"</c>.</param>
    /// <param name="timeZone">The zone the schedule is read in; UTC when omitted.</param>
    /// <param name="queue">The queue to run in; the default queue when omitted.</param>
    string RunRecurring<TJob>(string jobId, string cron, TimeZoneInfo? timeZone = null, string? queue = null)
        where TJob : IRecurringJob;

    void TriggerRecurringJob(string id);

    void RemoveRecurringJob(string id);
}
