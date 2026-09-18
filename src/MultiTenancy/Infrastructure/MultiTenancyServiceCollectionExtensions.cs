using Aviant.Application.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aviant.Infrastructure.MultiTenancy;

public static class MultiTenancyServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the tenant scope. <see cref="ITenantScope" /> reads the background scope once a job has entered a
    ///     tenant, and <typeparamref name="TRequestScope" /> otherwise.
    /// </summary>
    /// <remarks>
    ///     The choice is made on every read, not when <see cref="ITenantScope" /> is resolved, so a context created in
    ///     a job's constructor, before the job entered its tenant, still follows the tenant.
    /// </remarks>
    /// <typeparam name="TRequestScope">Reads the tenant of the current request, for example from a claim.</typeparam>
    public static IServiceCollection AddAviantMultiTenancy<TRequestScope>(this IServiceCollection services)
        where TRequestScope : class, ITenantScope
    {
        services.TryAddScoped<IBackgroundTenantScope, BackgroundTenantScope>();
        services.TryAddScoped<TRequestScope>();
        services.TryAddScoped<ITenantScope, CurrentTenantScope<TRequestScope>>();

        return services;
    }

    private sealed class CurrentTenantScope<TRequestScope>(IBackgroundTenantScope background, TRequestScope request)
        : ITenantScope
        where TRequestScope : ITenantScope
    {
        private ITenantScope Current => background.Entered ? background : request;

        public Guid? TenantId => Current.TenantId;

        public bool CanCrossTenantBoundary => Current.CanCrossTenantBoundary;
    }
}
