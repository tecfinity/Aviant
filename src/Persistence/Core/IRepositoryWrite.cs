using System.Linq.Expressions;
using Aviant.Core.Entities;

namespace Aviant.Core.Persistence;

/// <summary>
///     Stages changes to entities of one type. Changes are saved by the unit of work, not here.
/// </summary>
/// <remarks>
///     An entity is validated (<see cref="Entity{TKey}.ValidateAsync" />) before it is staged;
///     one that reports itself invalid is refused with a <c>DomainRuleException</c>.
/// </remarks>
public interface IRepositoryWrite<TEntity, TPrimaryKey>
    where TEntity : Entity<TPrimaryKey>
{
    #region Insert

    public Task<TEntity> InsertAsync(
        TEntity           entity,
        CancellationToken cancellationToken = default);

    public Task<TPrimaryKey> InsertAndGetIdAsync(
        TEntity           entity,
        CancellationToken cancellationToken = default);

    public Task<TEntity> InsertOrUpdateAsync(
        TEntity           entity,
        CancellationToken cancellationToken = default);

    public Task<TPrimaryKey> InsertOrUpdateAndGetIdAsync(
        TEntity           entity,
        CancellationToken cancellationToken = default);

    #endregion

    #region Update

    public Task<TEntity> UpdateAsync(
        TEntity           entity,
        CancellationToken cancellationToken = default);

    public Task<TEntity> UpdateAsync(
        TPrimaryKey         id,
        Func<TEntity, Task> updateAction,
        CancellationToken   cancellationToken = default);

    #endregion

    #region Delete

    public Task DeleteAsync(
        TEntity           entity,
        CancellationToken cancellationToken = default);

    public Task DeleteAsync(
        TPrimaryKey       id,
        CancellationToken cancellationToken = default);

    public Task DeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default);

    #endregion
}
