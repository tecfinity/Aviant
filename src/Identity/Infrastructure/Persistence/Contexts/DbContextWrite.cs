using Aviant.Application.Persistence;
using Aviant.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Aviant.Infrastructure.Identity.Persistence.Contexts;

/// <summary>
///     A write context that also records which user created, changed or deleted each audited entity.
/// </summary>
public abstract class DbContextWrite<TDbContext>
    : Aviant.Infrastructure.Persistence.Contexts.DbContextWrite<TDbContext>
    where TDbContext : class, IDbContextWrite
{
    /// <inheritdoc />
    protected DbContextWrite(DbContextOptions options)
        : base(options)
    { }

    /// <inheritdoc />
    protected override AuditingInterceptor Auditing => UserAuditingInterceptor.Instance;
}
