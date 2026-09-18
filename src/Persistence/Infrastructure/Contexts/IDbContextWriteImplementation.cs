using System.Reflection;
using Aviant.Application.Persistence;
using Aviant.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Aviant.Infrastructure.Persistence.Contexts;

public interface IDbContextWriteImplementation<TDbContext>
    where TDbContext : class, IDbContextWrite
{
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
                    // Prevent update if the entity is read only
                    if (entry.Entity is IReadOnly)
                        throw new DbUpdateException("Entity is read only");

                    auditableImplementation.SetUpdateAuditProperties(entry);
                    break;

                case EntityState.Deleted:
                    auditableImplementation.CancelDeletionForSoftDelete(entry);
                    auditableImplementation.SetDeletionAuditProperties(entry);
                    break;

                // Entities that were loaded but not modified, or are no longer tracked, have
                // nothing to audit.
                case EntityState.Detached:
                case EntityState.Unchanged:
                default:
                    break;
            }
    }

    public void OnPreBaseModelCreating(
        ModelBuilder      modelBuilder,
        HashSet<Assembly> configurationAssemblies)
    {
        // By default add the assembly of the derived DbContext object
        // so that if the entity configuration is in the same assembly
        // as the derived DbContext object, then you don't have to use
        // AddConfigurationAssemblyFromEntity method to specify entity
        // configuration assemblies
        configurationAssemblies.Add(GetType().Assembly);

        foreach (var assembly in configurationAssemblies)
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    }

    public void OnPostBaseModelCreating(
        ModelBuilder                         modelBuilder,
        IAuditableImplementation<TDbContext> auditableImplementation)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            auditableImplementation.ConfigureGlobalFiltersMethodInfo?
               .MakeGenericMethod(entityType.ClrType)
               .Invoke(this, new object[] { modelBuilder, entityType });
    }
}
