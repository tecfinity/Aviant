using System.Reflection;

namespace Aviant.Infrastructure.Jobs;

/// <summary>
///     What <see cref="JobsServiceCollectionExtensions.AddAviantJobs" /> registers and checks.
/// </summary>
public sealed class AviantJobsOptions
{
    internal List<Assembly> Assemblies { get; } = [];

    internal List<Type> CheckedTypes { get; } = [];

    /// <summary>
    ///     Stop the host when a job cannot be constructed. Off by default: a worker that runs its healthy jobs and
    ///     reports the broken one is better than a worker that does not start.
    /// </summary>
    public bool FailOnUnresolvableJobs { get; set; }

    /// <summary>
    ///     Registers every concrete <c>IJob&lt;T&gt;</c> and <c>IRecurringJob</c> in <paramref name="assemblies" />,
    ///     and checks at startup that each can be constructed.
    /// </summary>
    public AviantJobsOptions AddAssemblies(params Assembly[] assemblies)
    {
        Assemblies.AddRange(assemblies);

        return this;
    }

    /// <summary>
    ///     Also checks at startup that these job types resolve, for jobs registered by interface.
    /// </summary>
    public AviantJobsOptions Validate(params Type[] jobTypes)
    {
        CheckedTypes.AddRange(jobTypes);

        return this;
    }
}
