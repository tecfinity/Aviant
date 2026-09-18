using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Aviant.Core.Entities;
using Aviant.Core.Persistence;
using Aviant.Infrastructure.Persistence.Contexts;

namespace Aviant.Infrastructure.Persistence.Repository;

/// <summary>
///     A repository that both reads and writes through one write context, for domains that do
///     not split reads onto a separate context.
/// </summary>
/// <remarks>
///     Writes come from <see cref="RepositoryWriteBase{TDbContext,TEntity,TPrimaryKey}" />; reads
///     delegate to the same implementation as <see cref="RepositoryRead{TDbContext,TEntity,TPrimaryKey}" />,
///     so both use real EF Core async queries.
/// </remarks>
public abstract class Repository<TDbContext, TEntity, TPrimaryKey>
    : RepositoryWrite<TDbContext, TEntity, TPrimaryKey>,
      IRepositoryRead<TEntity, TPrimaryKey>
    where TEntity : Entity<TPrimaryKey>
    where TDbContext : DbContextWrite<TDbContext>
{
    private readonly Reader _reader;

    protected Repository(TDbContext context)
        : base(context) => _reader = new Reader(context);

    public virtual IQueryable<TEntity> GetAll() => _reader.GetAll();

    public virtual IQueryable<TEntity> GetAllIncluding(params Expression<Func<TEntity, object>>[] propertySelectors) =>
        _reader.GetAllIncluding(propertySelectors);

    public virtual IQueryable<TEntity> FindBy(Expression<Func<TEntity, bool>> predicate) => _reader.FindBy(predicate);

    public virtual IQueryable<TEntity> FindByIncluding(
        Expression<Func<TEntity, bool>>            predicate,
        params Expression<Func<TEntity, object>>[] propertySelectors) =>
        _reader.FindByIncluding(predicate, propertySelectors);

    public virtual Collection<TEntity> GetAllList() => _reader.GetAllList();

    public virtual Collection<TEntity> GetAllList(Expression<Func<TEntity, bool>> predicate) => _reader.GetAllList(predicate);

    public virtual ValueTask<Collection<TEntity>> GetAllListAsync(CancellationToken cancellationToken = default) =>
        _reader.GetAllListAsync(cancellationToken);

    public virtual ValueTask<Collection<TEntity>> GetAllListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default) =>
        _reader.GetAllListAsync(predicate, cancellationToken);

    public virtual TEntity GetAllListIncluding(
        Expression<Func<TEntity, bool>>            predicate,
        params Expression<Func<TEntity, object>>[] propertySelectors) =>
        _reader.GetAllListIncluding(predicate, propertySelectors);

    public virtual ValueTask<TEntity> GetAllListIncludingAsync(
        Expression<Func<TEntity, bool>>            predicate,
        CancellationToken                          cancellationToken = default,
        params Expression<Func<TEntity, object>>[] propertySelectors) =>
        _reader.GetAllListIncludingAsync(predicate, cancellationToken, propertySelectors);

    public virtual TEntity Get(TPrimaryKey id) => _reader.Get(id);

    public virtual ValueTask<TEntity> GetAsync(TPrimaryKey id, CancellationToken cancellationToken = default) =>
        _reader.GetAsync(id, cancellationToken);

    public virtual TEntity Single(Expression<Func<TEntity, bool>> predicate) => _reader.Single(predicate);

    public virtual ValueTask<TEntity> GetSingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default) =>
        _reader.GetSingleAsync(predicate, cancellationToken);

    public virtual TEntity GetSingleIncluding(
        Expression<Func<TEntity, bool>>            predicate,
        params Expression<Func<TEntity, object>>[] propertySelectors) =>
        _reader.GetSingleIncluding(predicate, propertySelectors);

    public virtual ValueTask<TEntity> GetSingleIncludingAsync(
        Expression<Func<TEntity, bool>>            predicate,
        CancellationToken                          cancellationToken = default,
        params Expression<Func<TEntity, object>>[] propertySelectors) =>
        _reader.GetSingleIncludingAsync(predicate, cancellationToken, propertySelectors);

    public virtual TEntity FirstOrDefault(TPrimaryKey id) => _reader.FirstOrDefault(id);

    public virtual TEntity FirstOrDefault(Expression<Func<TEntity, bool>> predicate) => _reader.FirstOrDefault(predicate);

    public virtual ValueTask<TEntity> FirstOrDefaultAsync(TPrimaryKey id, CancellationToken cancellationToken = default) =>
        _reader.FirstOrDefaultAsync(id, cancellationToken);

    public virtual ValueTask<TEntity> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default) =>
        _reader.FirstOrDefaultAsync(predicate, cancellationToken);

    public virtual TEntity FirstOrDefaultIncluding(
        TPrimaryKey                                id,
        params Expression<Func<TEntity, object>>[] propertySelectors) =>
        _reader.FirstOrDefaultIncluding(id, propertySelectors);

    public virtual TEntity FirstOrDefaultIncluding(
        Expression<Func<TEntity, bool>>            predicate,
        params Expression<Func<TEntity, object>>[] propertySelectors) =>
        _reader.FirstOrDefaultIncluding(predicate, propertySelectors);

    public virtual ValueTask<TEntity> FirstOrDefaultIncludingAsync(
        TPrimaryKey                                id,
        CancellationToken                          cancellationToken = default,
        params Expression<Func<TEntity, object>>[] propertySelectors) =>
        _reader.FirstOrDefaultIncludingAsync(id, cancellationToken, propertySelectors);

    public virtual ValueTask<TEntity> FirstOrDefaultIncludingAsync(
        Expression<Func<TEntity, bool>>            predicate,
        CancellationToken                          cancellationToken = default,
        params Expression<Func<TEntity, object>>[] propertySelectors) =>
        _reader.FirstOrDefaultIncludingAsync(predicate, cancellationToken, propertySelectors);

    public virtual ValueTask<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default) =>
        _reader.AnyAsync(predicate, cancellationToken);

    public virtual ValueTask<bool> AllAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default) =>
        _reader.AllAsync(predicate, cancellationToken);

    public virtual int Count() => _reader.Count();

    public virtual int Count(Expression<Func<TEntity, bool>> predicate) => _reader.Count(predicate);

    public virtual ValueTask<int> CountAsync(CancellationToken cancellationToken = default) => _reader.CountAsync(cancellationToken);

    public virtual ValueTask<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default) =>
        _reader.CountAsync(predicate, cancellationToken);

    public virtual long LongCount() => _reader.LongCount();

    public virtual long LongCount(Expression<Func<TEntity, bool>> predicate) => _reader.LongCount(predicate);

    public virtual ValueTask<long> LongCountAsync(CancellationToken cancellationToken = default) =>
        _reader.LongCountAsync(cancellationToken);

    public virtual ValueTask<long> LongCountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default) =>
        _reader.LongCountAsync(predicate, cancellationToken);

    private sealed class Reader(TDbContext context) : RepositoryReadBase<TDbContext, TEntity, TPrimaryKey>(context);
}
