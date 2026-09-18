using System.Reflection;
using Aviant.Application.Identity;
using Aviant.Application.Persistence;
using Aviant.Core.Entities;
using Aviant.Infrastructure.Persistence.Configurations;
using Aviant.Infrastructure.Persistence.Contexts;
using Aviant.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;

namespace Aviant.Infrastructure.Identity.Persistence.Contexts;

public abstract class AuthorizationDbContextWrite<TDbContext, TApplicationUser, TApplicationRole>
    : AuthorizationDbContext<TApplicationUser, TApplicationRole, Guid>,
      IDbContextWrite
    where TDbContext : class, IDbContextWrite
    where TApplicationUser : ApplicationUser
    where TApplicationRole : ApplicationRole
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly HashSet<Assembly> ConfigurationAssemblies = [];

    protected AuthorizationDbContextWrite(DbContextOptions options)
        : base(options) => ChangeTracker.LazyLoadingEnabled = false;

    /// <summary>
    ///     The interceptor that audits this context's saves.
    /// </summary>
    protected virtual AuditingInterceptor Auditing => UserAuditingInterceptor.Instance;

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
        foreach (var assembly in ConfigurationAssemblies.Append(GetType().Assembly).Distinct())
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);

        base.OnModelCreating(modelBuilder);

        modelBuilder.UseSoftDeleteFilter();
    }
}
