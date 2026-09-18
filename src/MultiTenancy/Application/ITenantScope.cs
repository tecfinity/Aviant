namespace Aviant.Application.MultiTenancy;

/// <summary>
///     The tenant the current unit of work acts for: a request, a job or a command-line run.
/// </summary>
public interface ITenantScope
{
    /// <summary>
    ///     The current tenant, or <see langword="null" /> when none has been chosen.
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    ///     Whether this caller may see every tenant's data, as a platform operator can. With no tenant chosen, such a
    ///     caller reads across tenants; with one chosen, it is scoped like anybody else.
    /// </summary>
    bool CanCrossTenantBoundary { get; }
}
