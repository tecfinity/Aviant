namespace Aviant.Core.MultiTenancy;

/// <summary>
///     An entity that belongs to one tenant. The tenant is stamped when it is first saved and never changes.
/// </summary>
public interface ITenantOwned
{
    /// <summary>
    ///     The owning tenant, or <see langword="null" /> for a row written before tenancy existed.
    /// </summary>
    Guid? TenantId { get; }
}
