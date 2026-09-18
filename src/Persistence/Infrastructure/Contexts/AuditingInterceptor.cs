using Aviant.Core.Entities;
using Aviant.Core.Timing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Aviant.Infrastructure.Persistence.Contexts;

/// <summary>
///     Stamps audit times, turns deletes of <see cref="ISoftDelete" /> entities into soft deletes, and refuses
///     changes to <see cref="IReadOnly" /> entities, on every save, synchronous or not.
/// </summary>
/// <remarks>
///     <see cref="DbContextWrite{TDbContext}" /> adds it on its own. Any other context can opt in with
///     <c>optionsBuilder.AddInterceptors(AuditingInterceptor.Instance)</c>. Times come from
///     <see cref="Clock.TimeProvider" />.
/// </remarks>
public class AuditingInterceptor : SaveChangesInterceptor
{
    /// <summary>
    ///     A shared instance; the interceptor holds no state.
    /// </summary>
    public static AuditingInterceptor Instance { get; } = new();

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Audit(eventData.Context);

        return result;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData     eventData,
        InterceptionResult<int> result,
        CancellationToken      cancellationToken = default)
    {
        Audit(eventData.Context);

        return ValueTask.FromResult(result);
    }

    /// <summary>
    ///     Stamps an entity that is about to be inserted.
    /// </summary>
    protected virtual void OnAdded(DbContext context, EntityEntry entry, DateTimeOffset now)
    {
        if (entry.Entity is IHasCreationTime created && created.Created == default)
            created.Created = now;
    }

    /// <summary>
    ///     Stamps an entity that is about to be updated.
    /// </summary>
    protected virtual void OnModified(DbContext context, EntityEntry entry, DateTimeOffset now)
    {
        if (entry.Entity is IReadOnly)
            throw new DbUpdateException($"{entry.Entity.GetType().Name} is read-only and cannot be changed.");

        if (entry.Entity is IHasUpdatedTime updated)
            updated.Updated = now;
    }

    /// <summary>
    ///     Called for an entity about to be deleted. A soft-deletable entity is kept and marked deleted instead.
    /// </summary>
    protected virtual void OnDeleted(DbContext context, EntityEntry entry, DateTimeOffset now)
    {
        if (entry.Entity is not ISoftDelete softDelete)
            return;

        // Keep the row: write only the deletion markers.
        entry.State          = EntityState.Unchanged;
        softDelete.IsDeleted = true;
        entry.Property(nameof(ISoftDelete.IsDeleted)).IsModified = true;

        if (entry.Entity is IHasDeletionTime deletion)
        {
            deletion.Deleted ??= now;
            entry.Property(nameof(IHasDeletionTime.Deleted)).IsModified = true;
        }
    }

    private void Audit(DbContext? context)
    {
        if (context is null)
            return;

        var now = Clock.TimeProvider.GetUtcNow();

        // Materialised first: soft deletes change entry states while we walk them.
        foreach (var entry in context.ChangeTracker.Entries().ToList())
            switch (entry.State)
            {
                case EntityState.Added:
                    OnAdded(context, entry, now);
                    break;

                case EntityState.Modified:
                    OnModified(context, entry, now);
                    break;

                case EntityState.Deleted:
                    OnDeleted(context, entry, now);
                    break;
            }
    }
}
