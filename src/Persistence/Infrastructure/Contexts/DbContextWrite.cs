using System.Reflection;
using Aviant.Application.Persistence;
using Aviant.Core.Entities;
using Aviant.Infrastructure.Persistence.Configurations;
using Aviant.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;

namespace Aviant.Infrastructure.Persistence.Contexts;

/// <summary>
///     A context for writes: applies the entity configurations, audits every save through
///     <see cref="Auditing" />, and hides soft-deleted rows.
/// </summary>
public abstract class DbContextWrite<TDbContext> : DbContext, IDbContextWrite
    where TDbContext : class, IDbContextWrite
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly HashSet<Assembly> ConfigurationAssemblies = [];

    protected DbContextWrite(DbContextOptions options)
        : base(options) => ChangeTracker.LazyLoadingEnabled = false;

    /// <summary>
    ///     The interceptor that audits this context's saves.
    /// </summary>
    protected virtual AuditingInterceptor Auditing => AuditingInterceptor.Instance;

    public static void AddConfigurationAssemblyFromEntity<TEntity, TKey>(
        EntityConfiguration<TEntity, TKey> entityConfiguration)
        where TEntity : Entity<TKey>
    {
        ConfigurationAssemblies.Add(entityConfiguration.GetType().Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(Auditing);

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // The derived context's own assembly, plus any registered with AddConfigurationAssemblyFromEntity.
        foreach (var assembly in ConfigurationAssemblies.Append(GetType().Assembly).Distinct())
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);

        base.OnModelCreating(modelBuilder);

        modelBuilder.UseSoftDeleteFilter();
    }
}
