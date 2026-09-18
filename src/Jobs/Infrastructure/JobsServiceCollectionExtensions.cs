using Aviant.Application.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Aviant.Infrastructure.Jobs;

public static class JobsServiceCollectionExtensions
{
    /// <summary>
    ///     Registers <see cref="IJobRunner" />, the jobs in the configured assemblies, and a startup check that every
    ///     job can be constructed.
    /// </summary>
    /// <remarks>
    ///     Hangfire builds a job only when it runs, so a missing dependency otherwise shows up as a failed job in
    ///     production, and each missing dependency hides the next. Hangfire itself (storage and server) is
    ///     registered separately with <c>AddHangfire</c>.
    /// </remarks>
    public static IServiceCollection AddAviantJobs(
        this IServiceCollection      services,
        Action<AviantJobsOptions>? configure = null)
    {
        var options = new AviantJobsOptions();
        configure?.Invoke(options);

        services.TryAddSingleton<IJobRunner, JobRunner>();

        List<Type> jobs = options.Assemblies
           .Distinct()
           .SelectMany(assembly => assembly.GetTypes())
           .Where(type => type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false } && IsJob(type))
           .ToList();

        foreach (var job in jobs)
            services.TryAddTransient(job);

        Type[] checkedTypes = jobs.Concat(options.CheckedTypes).Distinct().ToArray();

        if (checkedTypes.Length > 0)
            services.AddHostedService(provider => new JobConstructionCheck(
                provider,
                checkedTypes,
                options.FailOnUnresolvableJobs,
                provider.GetRequiredService<ILogger<JobConstructionCheck>>()));

        return services;
    }

    private static bool IsJob(Type type) =>
        typeof(IRecurringJob).IsAssignableFrom(type)
     || type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IJob<>));
}
