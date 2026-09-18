using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Aviant.Infrastructure.Persistence.Contexts;
using Aviant.Core.Entities;
using Aviant.Core.Exceptions;
using Aviant.Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aviant.Infrastructure.Persistence.Repository;

/// <inheritdoc cref="Aviant.Core.Persistence.IRepositoryRead{TEntity,TPrimaryKey}" />
public abstract class RepositoryReadBase<TDbContext, TEntity, TPrimaryKey>
    : IRepositoryRead<TEntity, TPrimaryKey>,
      IRepositoryImplementation<TEntity, TPrimaryKey>
    where TDbContext : DbContext
    where TEntity : Entity<TPrimaryKey>
{
    private readonly IRepositoryImplementation<TEntity, TPrimaryKey> _repositoryImplementation;

    protected RepositoryReadBase(TDbContext dbContext)
    {
        _repositoryImplementation = this;

        DbContext = dbContext;
    }

    /// <summary>The context this repository reads from; it belongs to its dependency scope and is never disposed here.</summary>
    protected TDbContext DbContext { get; }

    protected DbSet<TEntity> DbSet => DbContext.Set<TEntity>();

    #region Select/Get/Query

    public virtual IQueryable<TEntity> GetAll() => DbSet;

    public virtual IQueryable<TEntity> GetAllIncluding(params Expression<Func<TEntity, object>>[] propertySelectors)
    {
        IQueryable<TEntity> query = GetAll();

        return propertySelectors.Aggregate(
            query,
            (current, includeProperty) =>
                current.Include(includeProperty));
    }

    public virtual IQueryable<TEntity> FindBy(Expression<Func<TEntity, bool>> predicate) =>
        GetAll().Where(predicate);

    public virtual IQueryable<TEntity> FindByIncluding(
        Expression<Func<TEntity, bool>>            predicate,
        params Expression<Func<TEntity, object>>[] propertySelectors) =>
        GetAllIncluding(propertySelectors).Where(predicate);

    public virtual Collection<TEntity> GetAllList() =>
        new(GetAll().ToList());

    public virtual Collection<TEntity> GetAllList(Expression<Func<TEntity, bool>> predicate) =>
        new(FindBy(predicate).ToList());

    public virtual async ValueTask<Collection<TEntity>> GetAllListAsync(CancellationToken cancellationToken = default) =>
        new(await GetAll().ToListAsync(cancellationToken).ConfigureAwait(false));

    public virtual async ValueTask<Collection<TEntity>> GetAllListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default) =>
        new(await FindBy(predicate).ToListAsync(cancellationToken).ConfigureAwait(false));


    public virtual TEntity GetAllListIncluding(
        Expression<Func<TEntity, bool>>            predicate,
        params Expression<Func<TEntity, object>>[] propertySelectors) =>
        FindByIncluding(predicate, propertySelectors)
           .FirstOrDefault(predicate)
     ?? throw new EntityNotFoundException(nameof(predicate));

    public virtual async ValueTask<TEntity> GetAllListIncludingAsync(
        Expression<Func<TEntity, bool>>            predicate,
        CancellationToken                          cancellationToken = default,
        params Expression<Func<TEntity, object>>[] propertySelectors) =>
        await FindByIncluding(predicate, propertySelectors)
           .FirstOrDefaultAsync(predicate, cancellationToken)
           .ConfigureAwait(false)
     ?? throw new EntityNotFoundException(nameof(predicate));

    public virtual TEntity Get(TPrimaryKey id) =>
        FirstOrDefault(id)
     ?? throw new EntityNotFoundException(typeof(TEntity), id);

    public virtual async ValueTask<TEntity> GetAsync(
        TPrimaryKey       id,
        CancellationToken cancellationToken = default) =>
        await GetAll()
           .FirstOrDefaultAsync(_repositoryImplementation.CreateEqualityExpressionForId(id), cancellationToken)
           .ConfigureAwait(false)
     ?? throw new EntityNotFoundException(typeof(TEntity), id);


    public virtual TEntity Single(Expression<Func<TEntity, bool>> predicate) =>
        GetAll().Single(predicate);

    public virtual async ValueTask<TEntity> GetSingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default) =>
        await GetAll().SingleAsync(predicate, cancellationToken).ConfigureAwait(false);

    public virtual TEntity GetSingleIncluding(
        Expression<Func<TEntity, bool>>            predicate,
        params Expression<Func<TEntity, object>>[] propertySelectors) =>
        GetAllIncluding(propertySelectors)
           .SingleOrDefault(predicate)
     ?? throw new EntityNotFoundException(nameof(predicate));

    public virtual async ValueTask<TEntity> GetSingleIncludingAsync(
        Expression<Func<TEntity, bool>>            predicate,
        CancellationToken                          cancellationToken = default,
        params Expression<Func<TEntity, object>>[] propertySelectors) =>
        await GetAllIncluding(propertySelectors)
           .SingleOrDefaultAsync(predicate, cancellationToken)
           .ConfigureAwait(false)
     ?? throw new EntityNotFoundException(nameof(predicate));

    public virtual TEntity FirstOrDefault(TPrimaryKey id) =>
        GetAll().FirstOrDefault(_repositoryImplementation.CreateEqualityExpressionForId(id))
     ?? throw new EntityNotFoundException(nameof(id));

    public virtual TEntity FirstOrDefault(Expression<Func<TEntity, bool>> predicate) =>
        GetAll().FirstOrDefault(predicate)
     ?? throw new EntityNotFoundException(nameof(predicate));

    public virtual async ValueTask<TEntity> FirstOrDefaultAsync(
        TPrimaryKey       id,
        CancellationToken cancellationToken = default) =>
        await GetAll()
           .FirstOrDefaultAsync(_repositoryImplementation.CreateEqualityExpressionForId(id), cancellationToken)
           .ConfigureAwait(false)
     ?? throw new EntityNotFoundException(nameof(id));

    public virtual async ValueTask<TEntity> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default) =>
        await GetAll().FirstOrDefaultAsync(predicate, cancellationToken).ConfigureAwait(false)
     ?? throw new EntityNotFoundException(nameof(predicate));

    public virtual TEntity FirstOrDefaultIncluding(
        TPrimaryKey                                id,
        params Expression<Func<TEntity, object>>[] propertySelectors) =>
        GetAllIncluding(propertySelectors)
           .FirstOrDefault(_repositoryImplementation.CreateEqualityExpressionForId(id))
     ?? throw new EntityNotFoundException(nameof(id));

    public virtual TEntity FirstOrDefaultIncluding(
        Expression<Func<TEntity, bool>>            predicate,
        params Expression<Func<TEntity, object>>[] propertySelectors) =>
        GetAllIncluding(propertySelectors)
           .FirstOrDefault(predicate)
     ?? throw new EntityNotFoundException(nameof(predicate));

    public virtual async ValueTask<TEntity> FirstOrDefaultIncludingAsync(
        TPrimaryKey                                id,
        CancellationToken                          cancellationToken = default,
        params Expression<Func<TEntity, object>>[] propertySelectors) =>
        await GetAllIncluding(propertySelectors)
           .FirstOrDefaultAsync(_repositoryImplementation.CreateEqualityExpressionForId(id), cancellationToken)
           .ConfigureAwait(false)
     ?? throw new EntityNotFoundException(nameof(id));

    public virtual async ValueTask<TEntity> FirstOrDefaultIncludingAsync(
        Expression<Func<TEntity, bool>>            predicate,
        CancellationToken                          cancellationToken = default,
        params Expression<Func<TEntity, object>>[] propertySelectors) =>
        await GetAllIncluding(propertySelectors)
           .FirstOrDefaultAsync(predicate, cancellationToken)
           .ConfigureAwait(false)
     ?? throw new EntityNotFoundException(nameof(predicate));

    #endregion

    #region Aggregates

    public virtual ValueTask<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default) =>
        new(GetAll().AnyAsync(predicate, cancellationToken));

    public virtual ValueTask<bool> AllAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default) =>
        new(GetAll().AllAsync(predicate, cancellationToken));

    public virtual int Count() =>
        GetAll().Count();

    public virtual int Count(Expression<Func<TEntity, bool>> predicate) =>
        GetAll().Count(predicate);

    public virtual ValueTask<int> CountAsync(CancellationToken cancellationToken = default) =>
        new(GetAll().CountAsync(cancellationToken));

    public virtual ValueTask<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default) =>
        new(GetAll().CountAsync(predicate, cancellationToken));

    public virtual long LongCount() =>
        GetAll().LongCount();

    public virtual long LongCount(Expression<Func<TEntity, bool>> predicate) =>
        GetAll().LongCount(predicate);

    public virtual ValueTask<long> LongCountAsync(CancellationToken cancellationToken = default) =>
        new(GetAll().LongCountAsync(cancellationToken));

    public virtual ValueTask<long> LongCountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default) =>
        new(GetAll().LongCountAsync(predicate, cancellationToken));

    #endregion
}

public abstract class RepositoryRead<TDbContext, TEntity, TPrimaryKey>
    : RepositoryReadBase<TDbContext, TEntity, TPrimaryKey>
    where TEntity : Entity<TPrimaryKey>
    where TDbContext : DbContextRead
{
    protected RepositoryRead(TDbContext context)
        : base(context)
    { }
}
