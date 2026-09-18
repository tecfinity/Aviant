using System.Linq.Expressions;
using Aviant.Infrastructure.Persistence.Contexts;
using Aviant.Core.Entities;
using Aviant.Core.Exceptions;
using Aviant.Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aviant.Infrastructure.Persistence.Repository;

/// <inheritdoc cref="Aviant.Core.Persistence.IRepositoryWrite{TEntity,TPrimaryKey}" />
/// <remarks>
///     The context belongs to the dependency scope that created it; the repository never
///     disposes it.
/// </remarks>
public abstract class RepositoryWriteBase<TDbContext, TEntity, TPrimaryKey>
    : IRepositoryWrite<TEntity, TPrimaryKey>,
      IRepositoryImplementation<TEntity, TPrimaryKey>
    where TDbContext : DbContext
    where TEntity : Entity<TPrimaryKey>
{
    private readonly IRepositoryImplementation<TEntity, TPrimaryKey> _repositoryImplementation;

    protected RepositoryWriteBase(TDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _repositoryImplementation = this;
        DbContext                 = dbContext;
    }

    /// <summary>The context this repository stages changes in, for queries the base does not cover.</summary>
    protected TDbContext DbContext { get; }

    protected DbSet<TEntity> DbSet => DbContext.Set<TEntity>();

    #region Insert

    public virtual async Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await EnsureValidAsync(entity, cancellationToken).ConfigureAwait(false);

        await DbSet.AddAsync(entity, cancellationToken).ConfigureAwait(false);

        return entity;
    }

    public virtual async Task<TPrimaryKey> InsertAndGetIdAsync(
        TEntity           entity,
        CancellationToken cancellationToken = default) =>
        (await InsertAsync(entity, cancellationToken).ConfigureAwait(false)).Id;

    public virtual async Task<TEntity> InsertOrUpdateAsync(
        TEntity           entity,
        CancellationToken cancellationToken = default) =>
        entity.IsTransient()
            ? await InsertAsync(entity, cancellationToken).ConfigureAwait(false)
            : await UpdateAsync(entity, cancellationToken).ConfigureAwait(false);

    public virtual async Task<TPrimaryKey> InsertOrUpdateAndGetIdAsync(
        TEntity           entity,
        CancellationToken cancellationToken = default) =>
        (await InsertOrUpdateAsync(entity, cancellationToken).ConfigureAwait(false)).Id;

    #endregion

    #region Update

    /// <summary>
    ///     Stages an update. A tracked entity keeps EF Core's change tracking, so only changed
    ///     columns are written; a detached one is attached as modified.
    /// </summary>
    public virtual async Task<TEntity> UpdateAsync(
        TEntity           entity,
        CancellationToken cancellationToken = default)
    {
        await EnsureValidAsync(entity, cancellationToken).ConfigureAwait(false);

        if (DbContext.Entry(entity).State == EntityState.Detached)
            DbSet.Update(entity);

        return entity;
    }

    public virtual async Task<TEntity> UpdateAsync(
        TPrimaryKey         id,
        Func<TEntity, Task> updateAction,
        CancellationToken   cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateAction);

        var entity = await GetAsync(id, cancellationToken).ConfigureAwait(false);

        await updateAction(entity).ConfigureAwait(false);

        return await UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Delete

    public virtual async Task DeleteAsync(
        TEntity           entity,
        CancellationToken cancellationToken = default)
    {
        await EnsureValidAsync(entity, cancellationToken).ConfigureAwait(false);

        DbSet.Remove(entity);
    }

    public virtual async Task DeleteAsync(
        TPrimaryKey       id,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetAsync(id, cancellationToken).ConfigureAwait(false);

        await DeleteAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task DeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default)
    {
        List<TEntity> entities = await DbSet.Where(predicate)
           .ToListAsync(cancellationToken)
           .ConfigureAwait(false);

        foreach (var entity in entities)
            await DeleteAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    private async Task<TEntity> GetAsync(TPrimaryKey id, CancellationToken cancellationToken) =>
        await DbSet
           .FirstOrDefaultAsync(_repositoryImplementation.CreateEqualityExpressionForId(id), cancellationToken)
           .ConfigureAwait(false)
     ?? throw new EntityNotFoundException(typeof(TEntity), id);

    private static async Task EnsureValidAsync(TEntity entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!await entity.ValidateAsync(cancellationToken).ConfigureAwait(false))
            throw new DomainRuleException($"{typeof(TEntity).Name} {entity.Id} is not valid.");
    }
}

public abstract class RepositoryWrite<TDbContext, TEntity, TPrimaryKey>
    : RepositoryWriteBase<TDbContext, TEntity, TPrimaryKey>
    where TEntity : Entity<TPrimaryKey>
    where TDbContext : DbContextWrite<TDbContext>
{
    protected RepositoryWrite(TDbContext context)
        : base(context)
    { }
}
