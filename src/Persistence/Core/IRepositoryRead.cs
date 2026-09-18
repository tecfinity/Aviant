using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Aviant.Core.Entities;

namespace Aviant.Core.Persistence;

/// <inheritdoc />
/// <summary>
///     This interface is implemented by all read repositories to ensure implementation of fixed methods.
/// </summary>
/// <typeparam name="TEntity">Main Entity type this repository works on</typeparam>
/// <typeparam name="TPrimaryKey">Primary key type of the entity</typeparam>
public interface IRepositoryRead<TEntity, in TPrimaryKey>
    where TEntity : Entity<TPrimaryKey>
{
    #region Select/Get/Query

    /// <summary>
    ///     Used to get a IQueryable that is used to retrieve entities from entire table.
    /// </summary>
    /// <returns>IQueryable to be used to select entities from database</returns>
    public IQueryable<TEntity> GetAll();

    /// <summary>
    ///     Used to get a IQueryable that is used to retrieve entities from entire table.
    ///     One or more
    /// </summary>
    /// <param name="propertySelectors">A list of include expressions.</param>
    /// <returns>IQueryable to be used to select entities from database</returns>
    public IQueryable<TEntity> GetAllIncluding(params Expression<Func<TEntity, object>>[] propertySelectors);

    /// <summary>
    ///     Gets an entity with given predicate.
    /// </summary>
    /// <param name="predicate">Predicate to filter entities</param>
    /// <returns>IQueryable to be used to select entities from database</returns>
    public IQueryable<TEntity> FindBy(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    ///     Gets an entity with given predicate and retrieves entities from entire table.
    /// </summary>
    /// <param name="predicate">Predicate to filter entities</param>
    /// <param name="propertySelectors">A list of include expressions.</param>
    /// <returns>IQueryable to be used to select entities from database</returns>
    public IQueryable<TEntity> FindByIncluding(
        Expression<Func<TEntity, bool>>            predicate,
        params Expression<Func<TEntity, object>>[] propertySelectors);

    /// <summary>
    ///     Used to get all entities.
    /// </summary>
    /// <returns>Collection of all entities</returns>
    public Collection<TEntity> GetAllList();

    /// <summary>
    ///     Used to get all entities based on given <paramref name="predicate" />.
    /// </summary>
    /// <param name="predicate">A condition to filter entities</param>
    /// <returns>Collection of all entities</returns>
    public Collection<TEntity> GetAllList(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    ///     Used to get all entities.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>Collection of all entities</returns>
    public ValueTask<Collection<TEntity>> GetAllListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Used to get all entities based on given <paramref name="predicate" />.
    /// </summary>
    /// <param name="predicate">A condition to filter entities</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Collection of all entities</returns>
    public ValueTask<Collection<TEntity>> GetAllListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default);

    /// <summary>
    ///     Used to get a IQueryable that is used to retrieve all entities from entire table
    ///     based on given <paramref name="predicate" />.
    /// </summary>
    /// <param name="predicate">A condition to filter entities</param>
    /// <param name="propertySelectors">A list of include expressions.</param>
    /// <returns>List of all entities</returns>
    public TEntity GetAllListIncluding(
        Expression<Func<TEntity, bool>>            predicate,
        params Expression<Func<TEntity, object>>[] propertySelectors);

    /// <summary>
    ///     Used to get a IQueryable that is used to retrieve all entities from entire table
    ///     based on given <paramref name="predicate" />.
    /// </summary>
    /// <param name="predicate">A condition to filter entities</param>
    /// <param name="cancellationToken"></param>
    /// <param name="propertySelectors">A list of include expressions.</param>
    /// <returns>List of all entities</returns>
    public ValueTask<TEntity> GetAllListIncludingAsync(
        Expression<Func<TEntity, bool>>            predicate,
        CancellationToken                          cancellationToken = default,
        params Expression<Func<TEntity, object>>[] propertySelectors);

    /// <summary>
    ///     Gets an entity with given primary key.
    /// </summary>
    /// <param name="id">Primary key of the entity to get</param>
    /// <returns>Entity</returns>
    public TEntity Get(TPrimaryKey id);

    /// <summary>
    ///     Gets an entity with given primary key.
    /// </summary>
    /// <param name="id">Primary key of the entity to get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Entity</returns>
    public ValueTask<TEntity> GetAsync(
        TPrimaryKey       id,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets exactly one entity with given predicate.
    ///     Throws exception if no entity or more than one entity.
    /// </summary>
    /// <param name="predicate">Entity</param>
    public TEntity Single(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    ///     Gets exactly one entity with given predicate.
    ///     Throws exception if no entity or more than one entity.
    /// </summary>
    /// <param name="predicate">Entity</param>
    /// <param name="cancellationToken"></param>
    public ValueTask<TEntity> GetSingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default);

    /// <summary>
    ///     Gets exactly one entity with given predicate and retrieves entities from entire table.
    ///     Throws exception if no entity or more than one entity.
    /// </summary>
    /// <param name="predicate">Entity</param>
    /// <param name="propertySelectors">A list of include expressions.</param>
    /// <returns>IQueryable to be used to select entities from database</returns>
    public TEntity GetSingleIncluding(
        Expression<Func<TEntity, bool>>            predicate,
        params Expression<Func<TEntity, object>>[] propertySelectors);

    /// <summary>
    ///     Gets exactly one entity with given predicate and retrieves entities from entire table.
    ///     Throws exception if no entity or more than one entity.
    /// </summary>
    /// <param name="predicate">Entity</param>
    /// <param name="cancellationToken"></param>
    /// <param name="propertySelectors">A list of include expressions.</param>
    /// <returns>IQueryable to be used to select entities from database</returns>
    public ValueTask<TEntity> GetSingleIncludingAsync(
        Expression<Func<TEntity, bool>>            predicate,
        CancellationToken                          cancellationToken = default,
        params Expression<Func<TEntity, object>>[] propertySelectors);

    /// <summary>
    ///     Gets an entity with given primary key or null if not found.
    /// </summary>
    /// <param name="id">Primary key of the entity to get</param>
    /// <returns>Entity or null</returns>
    public TEntity FirstOrDefault(TPrimaryKey id);

    /// <summary>
    ///     Gets an entity with given given predicate or null if not found.
    /// </summary>
    /// <param name="predicate">Predicate to filter entities</param>
    public TEntity FirstOrDefault(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    ///     Gets an entity with given primary key or null if not found.
    /// </summary>
    /// <param name="id">Primary key of the entity to get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Entity or null</returns>
    public ValueTask<TEntity> FirstOrDefaultAsync(
        TPrimaryKey       id,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets an entity with given given predicate or null if not found.
    /// </summary>
    /// <param name="predicate">Predicate to filter entities</param>
    /// <param name="cancellationToken"></param>
    public ValueTask<TEntity> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default);

    /// <summary>
    ///     Gets an entity with given primary key and retrieves entities from entire table.
    /// </summary>
    /// <param name="id">Primary key of the entity to get</param>
    /// <param name="propertySelectors">A list of include expressions.</param>
    /// <returns>IQueryable to be used to select entities from database</returns>
    public TEntity FirstOrDefaultIncluding(
        TPrimaryKey                                id,
        params Expression<Func<TEntity, object>>[] propertySelectors);

    /// <summary>
    ///     Gets an entity with given predicate and retrieves entities from entire table.
    /// </summary>
    /// <param name="predicate">Predicate to filter entities</param>
    /// <param name="propertySelectors">A list of include expressions.</param>
    /// <returns>IQueryable to be used to select entities from database</returns>
    public TEntity FirstOrDefaultIncluding(
        Expression<Func<TEntity, bool>>            predicate,
        params Expression<Func<TEntity, object>>[] propertySelectors);

    /// <summary>
    ///     Gets an entity with given primary key and retrieves entities from entire table.
    /// </summary>
    /// <param name="id">Primary key of the entity to get</param>
    /// <param name="cancellationToken"></param>
    /// <param name="propertySelectors">A list of include expressions.</param>
    /// <returns>IQueryable to be used to select entities from database</returns>
    public ValueTask<TEntity> FirstOrDefaultIncludingAsync(
        TPrimaryKey                                id,
        CancellationToken                          cancellationToken = default,
        params Expression<Func<TEntity, object>>[] propertySelectors);

    /// <summary>
    ///     Gets an entity with given predicate and retrieves entities from entire table.
    /// </summary>
    /// <param name="predicate">Predicate to filter entities</param>
    /// <param name="cancellationToken"></param>
    /// <param name="propertySelectors">A list of include expressions.</param>
    /// <returns>IQueryable to be used to select entities from database</returns>
    public ValueTask<TEntity> FirstOrDefaultIncludingAsync(
        Expression<Func<TEntity, bool>>            predicate,
        CancellationToken                          cancellationToken = default,
        params Expression<Func<TEntity, object>>[] propertySelectors);

    #endregion

    #region Aggregates

    /// <summary>
    ///     Determines whether any element of a sequence satisfies a condition.
    /// </summary>
    /// <param name="predicate">Predicate to filter entities</param>
    /// <param name="cancellationToken"></param>
    /// <returns>True if there are any founds, false otherwise.</returns>
    public ValueTask<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default);

    /// <summary>
    ///     Determines whether all the elements of a sequence satisfy a condition.
    /// </summary>
    /// <param name="predicate">Predicate to filter entities</param>
    /// <param name="cancellationToken"></param>
    /// <returns>True if there are any founds, false otherwise.</returns>
    public ValueTask<bool> AllAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default);

    /// <summary>
    ///     Gets count of all entities in this repository.
    /// </summary>
    /// <returns>Count of entities</returns>
    public int Count();

    /// <summary>
    ///     Gets count of all entities in this repository based on given <paramref name="predicate" />.
    /// </summary>
    /// <param name="predicate">A method to filter count</param>
    /// <returns>Count of entities</returns>
    public int Count(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    ///     Gets count of all entities in this repository.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>Count of entities</returns>
    public ValueTask<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets count of all entities in this repository based on given <paramref name="predicate" />.
    /// </summary>
    /// <param name="predicate">A method to filter count</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Count of entities</returns>
    public ValueTask<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default);

    /// <summary>
    ///     Gets count of all entities in this repository (use if expected return value is greater than
    ///     <see cref="int.MaxValue" />.
    /// </summary>
    /// <returns>Count of entities</returns>
    public long LongCount();

    /// <summary>
    ///     Gets count of all entities in this repository based on given <paramref name="predicate" />
    ///     (use this overload if expected return value is greater than <see cref="int.MaxValue" />).
    /// </summary>
    /// <param name="predicate">A method to filter count</param>
    /// <returns>Count of entities</returns>
    public long LongCount(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    ///     Gets count of all entities in this repository (use if expected return value is greater than
    ///     <see cref="int.MaxValue" />.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>Count of entities</returns>
    public ValueTask<long> LongCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets count of all entities in this repository based on given <paramref name="predicate" />
    ///     (use this overload if expected return value is greater than <see cref="int.MaxValue" />).
    /// </summary>
    /// <param name="predicate">A method to filter count</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Count of entities</returns>
    public ValueTask<long> LongCountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken               cancellationToken = default);

    #endregion
}
