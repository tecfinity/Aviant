using Aviant.Application.Identity;
using Aviant.Application.Persistence;
using Aviant.Core.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Aviant.Infrastructure.Identity.Persistence.Contexts;

public interface IAuditableImplementation<TDbContext>
    : Infrastructure.Persistence.Contexts.IAuditableImplementation<TDbContext>
    where TDbContext : class, IDbContextWrite
{
    /// <summary>
    ///     The current user, resolved from the dependency scope that created this context; <see cref="Guid.Empty" />
    ///     when there is no user, as in background work.
    /// </summary>
    private Guid CurrentUserId =>
        this is DbContext context
            ? context.GetService<IDbContextOptions>()
                    .FindExtension<CoreOptionsExtension>()
                   ?.ApplicationServiceProvider
                   ?.GetService<ICurrentUserService>()
                   ?.UserId
             ?? Guid.Empty
            : Guid.Empty;

    #region Configure Audit Properties

    public new virtual void SetCreationAuditProperties(EntityEntry entry)
    {
        if (entry.Entity is not ICreationAudited creationAuditedEntity)
            return;

        if (creationAuditedEntity.CreatedBy != Guid.Empty)
            //CreatedUserId is already set
            return;

        creationAuditedEntity.CreatedBy = CurrentUserId;
    }

    public new virtual void SetUpdateAuditProperties(EntityEntry entry)
    {
        if (entry.Entity is not IUpdatedAudited updateAuditedEntity)
            return;

        if (updateAuditedEntity.UpdatedBy == CurrentUserId)
            //LastModifiedUserId is same as current user id
            return;

        updateAuditedEntity.UpdatedBy = CurrentUserId;
    }

    public new void SetDeletionAuditProperties(EntityEntry entry)
    {
        if (entry.Entity is not IDeletionAudited deletionAuditedEntity)
            return;

        deletionAuditedEntity.DeletedBy = CurrentUserId;
    }

    #endregion
}
