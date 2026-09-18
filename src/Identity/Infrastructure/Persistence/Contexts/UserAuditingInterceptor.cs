using Aviant.Application.Identity;
using Aviant.Core.Identity.Entities;
using Aviant.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Aviant.Infrastructure.Identity.Persistence.Contexts;

/// <summary>
///     Audits times like <see cref="AuditingInterceptor" />, and also records who created, changed or deleted an
///     entity.
/// </summary>
/// <remarks>
///     The user is the <see cref="ICurrentUserService" /> of the dependency scope that created the context, or
///     <see cref="Guid.Empty" /> when there is none, as in background work.
/// </remarks>
public class UserAuditingInterceptor : AuditingInterceptor
{
    /// <summary>
    ///     A shared instance; the interceptor holds no state.
    /// </summary>
    public new static UserAuditingInterceptor Instance { get; } = new();

    /// <inheritdoc />
    protected override void OnAdded(DbContext context, EntityEntry entry, DateTimeOffset now)
    {
        base.OnAdded(context, entry, now);

        if (entry.Entity is ICreationAudited created && created.CreatedBy == Guid.Empty)
            created.CreatedBy = CurrentUserId(context);
    }

    /// <inheritdoc />
    protected override void OnModified(DbContext context, EntityEntry entry, DateTimeOffset now)
    {
        base.OnModified(context, entry, now);

        if (entry.Entity is IUpdatedAudited updated)
            updated.UpdatedBy = CurrentUserId(context);
    }

    /// <inheritdoc />
    protected override void OnDeleted(DbContext context, EntityEntry entry, DateTimeOffset now)
    {
        base.OnDeleted(context, entry, now);

        if (entry.Entity is not IDeletionAudited deleted)
            return;

        deleted.DeletedBy = CurrentUserId(context);

        // A soft delete writes only the columns it marks.
        if (entry.State == EntityState.Unchanged)
            entry.Property(nameof(IDeletionAudited.DeletedBy)).IsModified = true;
    }

    private static Guid CurrentUserId(DbContext context) =>
        context.GetService<IDbContextOptions>()
           .FindExtension<CoreOptionsExtension>()
          ?.ApplicationServiceProvider
          ?.GetService<ICurrentUserService>()
          ?.UserId
     ?? Guid.Empty;
}
