using Aviant.Application.MultiTenancy;
using Aviant.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Aviant.Infrastructure.MultiTenancy;

/// <summary>
///     Stamps new tenant-owned rows with the current tenant and refuses to move a row to another tenant.
/// </summary>
/// <remarks>
///     The query filter keeps reads inside the tenant; this keeps writes there. Moving a row to another tenant is
///     never a legitimate edit: it is how a write would escape the boundary the filter enforces. Override
///     <see cref="IsOwned" /> and <see cref="OwnerProperty" /> for entities that name their owner differently.
/// </remarks>
public class TenantStampingInterceptor(ITenantScope scope) : SaveChangesInterceptor
{
    /// <summary>
    ///     The mapped property that holds the owning tenant.
    /// </summary>
    protected virtual string OwnerProperty => nameof(ITenantOwned.TenantId);

    /// <summary>
    ///     What the owner is called in error messages.
    /// </summary>
    protected virtual string OwnerName => "tenant";

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);

        return result;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData      eventData,
        InterceptionResult<int> result,
        CancellationToken       cancellationToken = default)
    {
        Stamp(eventData.Context);

        return ValueTask.FromResult(result);
    }

    /// <summary>
    ///     Whether <paramref name="entity" /> belongs to a tenant.
    /// </summary>
    protected virtual bool IsOwned(object entity) => entity is ITenantOwned;

    private void Stamp(DbContext? context)
    {
        if (context is null || scope.TenantId is not { } tenantId)
            return;

        foreach (EntityEntry entry in context.ChangeTracker.Entries())
        {
            if (!IsOwned(entry.Entity))
                continue;

            PropertyEntry owner = entry.Property(OwnerProperty);

            switch (entry.State)
            {
                case EntityState.Added when owner.CurrentValue is null:
                    owner.CurrentValue = tenantId;
                    break;

                case EntityState.Modified when owner.IsModified
                                            && owner.OriginalValue is Guid original
                                            && !Equals(owner.CurrentValue, original):
                    throw new InvalidOperationException(
                        $"Cannot move {entry.Entity.GetType().Name} from {OwnerName} {original} to {owner.CurrentValue}: "
                      + $"{OwnerName} ownership is immutable.");
            }
        }
    }
}
