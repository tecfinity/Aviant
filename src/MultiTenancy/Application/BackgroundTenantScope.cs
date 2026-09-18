namespace Aviant.Application.MultiTenancy;

/// <inheritdoc />
public sealed class BackgroundTenantScope : IBackgroundTenantScope
{
    /// <inheritdoc />
    public Guid? TenantId { get; private set; }

    /// <inheritdoc />
    public bool Entered => TenantId is not null;

    /// <inheritdoc />
    public bool CanCrossTenantBoundary => false;

    /// <inheritdoc />
    public void Enter(Guid tenantId)
    {
        if (TenantId is { } current && current != tenantId)
            throw new InvalidOperationException(
                $"This scope already acts for tenant {current} and cannot switch to {tenantId}. Use a new scope.");

        TenantId = tenantId;
    }
}
