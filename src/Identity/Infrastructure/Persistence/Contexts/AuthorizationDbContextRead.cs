using Aviant.Application.Identity;
using Aviant.Application.Persistence;
using Aviant.Infrastructure.Persistence.Contexts;
using Aviant.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;

namespace Aviant.Infrastructure.Identity.Persistence.Contexts;

public abstract class AuthorizationDbContextRead<TApplicationUser, TApplicationRole>
    : AuthorizationDbContext<TApplicationUser, TApplicationRole, Guid>,
      IDbContextRead,
      IDbContextReadImplementation
    where TApplicationUser : ApplicationUser
    where TApplicationRole : ApplicationRole
{
    private readonly IDbContextReadImplementation _readImplementation;

    protected AuthorizationDbContextRead(
        DbContextOptions                  options)
        : base(options)
    {
        // trait
        _readImplementation = this;

        TrackerSettings();
    }

    public override int SaveChanges()
    {
        IDbContextReadImplementation.ThrowWriteException();

        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        IDbContextReadImplementation.ThrowWriteException();

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool              acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = new())
    {
        IDbContextReadImplementation.ThrowWriteException();

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        IDbContextReadImplementation.ThrowWriteException();

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _readImplementation.OnPreBaseModelCreating(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.UseSoftDeleteFilter();
    }

    private void TrackerSettings() => IDbContextReadImplementation.TrackerSettings(ChangeTracker);
}
