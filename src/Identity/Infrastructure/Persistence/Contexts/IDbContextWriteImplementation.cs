using Aviant.Application.Persistence;
using Aviant.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Aviant.Infrastructure.Identity.Persistence.Contexts;

public interface IDbContextWriteImplementation<TDbContext>
    : Infrastructure.Persistence.Contexts.IDbContextWriteImplementation<TDbContext>
    where TDbContext : class, IDbContextWrite
{
    private Infrastructure.Persistence.Contexts.IDbContextWriteImplementation<TDbContext> Parent => this;

    public void ChangeTracker(
        ChangeTracker                        trackerChange,
        IAuditableImplementation<TDbContext> auditableImplementation)
    {
        foreach (EntityEntry<IAuditedEntity> entry in trackerChange.Entries<IAuditedEntity>())
            switch (entry.State)
            {
                case EntityState.Added:
                    auditableImplementation.SetCreationAuditProperties(entry);
                    break;

                case EntityState.Modified:
                    auditableImplementation.SetUpdateAuditProperties(entry);
                    break;

                case EntityState.Deleted:
                    auditableImplementation.SetDeletionAuditProperties(entry);
                    break;

                // Entities that were loaded but not modified, or are no longer tracked, have
                // nothing to audit.
                case EntityState.Detached:
                case EntityState.Unchanged:
                default:
                    break;
            }

        Parent.ChangeTracker(
            trackerChange,
            auditableImplementation);
    }
}
