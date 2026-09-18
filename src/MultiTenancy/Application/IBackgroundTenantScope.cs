namespace Aviant.Application.MultiTenancy;

/// <summary>
///     The tenant scope of background work, which has no request to read a tenant from. A job enters it once, before
///     it touches any tenant's data.
/// </summary>
public interface IBackgroundTenantScope : ITenantScope
{
    /// <summary>
    ///     Whether a tenant has been entered in this dependency scope.
    /// </summary>
    bool Entered { get; }

    /// <summary>
    ///     Acts for <paramref name="tenantId" /> for the rest of this dependency scope.
    /// </summary>
    /// <exception cref="InvalidOperationException">A different tenant was already entered.</exception>
    void Enter(Guid tenantId);
}
