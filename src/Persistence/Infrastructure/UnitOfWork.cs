using Aviant.Application.Persistence;

namespace Aviant.Infrastructure.Persistence;

/// <summary>
///     Saves the changes staged in <typeparamref name="TDbContext" />. The context belongs to the
///     dependency scope that created it, which also disposes it.
/// </summary>
public sealed class UnitOfWork<TDbContext> : IUnitOfWork<TDbContext>
    where TDbContext : IDbContextWrite
{
    private readonly TDbContext _context;

    public UnitOfWork(TDbContext context) => _context = context;

    public Task<int> CommitAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
