using Aviant.Application.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace Aviant.Application.MultiTenancy;

/// <summary>
///     The options of a job that works inside one tenant.
/// </summary>
public interface ITenantScopedJobOptions : IJobOptions
{
    /// <summary>
    ///     The tenant the job acts for, captured when it was enqueued.
    /// </summary>
    Guid TenantId { get; set; }
}

/// <summary>
///     A job that enters its tenant before it runs, so every query and save it makes is scoped the way the request
///     that enqueued it was.
/// </summary>
public abstract class TenantScopedJob<TOptions> : IJob<TOptions>
    where TOptions : class, ITenantScopedJobOptions
{
    private readonly IServiceProvider _services;

    /// <param name="services">The job's own dependency scope.</param>
    protected TenantScopedJob(IServiceProvider services) => _services = services;

    /// <inheritdoc />
    public async Task PerformAsync(TOptions jobOptions, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(jobOptions);

        _services.GetRequiredService<IBackgroundTenantScope>().Enter(jobOptions.TenantId);

        await RunAsync(jobOptions, _services, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Does the job's work inside its tenant.
    /// </summary>
    /// <param name="jobOptions">What the enqueuer asked for.</param>
    /// <param name="scope">The job's dependency scope, already inside the tenant. Resolve what the job needs from here.</param>
    /// <param name="cancellationToken">Cancelled when the job server shuts down.</param>
    protected abstract Task RunAsync(TOptions jobOptions, IServiceProvider scope, CancellationToken cancellationToken);
}
