using Aviant.Application.MultiTenancy;

namespace Aviant.Infrastructure.MultiTenancy;

/// <summary>
///     What a context's tenant query filter compares against. Hold one in a field of the context and pass it to
///     <see cref="TenantModelBuilderExtensions.UseTenantFilter" />.
/// </summary>
/// <remarks>
///     EF Core caches the model, and with it the query filters, across context instances. A filter re-reads values
///     reached through the context instance on every query, but bakes in any other object it captured. Keeping the
///     scope behind a member of the context is what keeps one tenant's filter from being reused for the next.
/// </remarks>
public sealed class TenantFilter(ITenantScope? scope)
{
    /// <summary>
    ///     The tenant rows must belong to.
    /// </summary>
    public Guid? CurrentTenantId => scope?.TenantId;

    /// <summary>
    ///     True when no tenant scope was supplied (design-time tools, migrations), or when a caller that may cross
    ///     tenants has not chosen one.
    /// </summary>
    public bool Unfiltered => scope is null || (scope.CanCrossTenantBoundary && scope.TenantId is null);
}
