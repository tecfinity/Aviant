using System.Linq.Expressions;
using Aviant.Core.Entities;

namespace Aviant.Infrastructure.Persistence.Repository;

internal interface IRepositoryImplementation<TEntity, in TPrimaryKey>
    where TEntity : Entity<TPrimaryKey>
{
    public Expression<Func<TEntity, bool>> CreateEqualityExpressionForId(TPrimaryKey id)
    {
        var lambdaParam = Expression.Parameter(typeof(TEntity));

        var lambdaBody = Expression.Equal(
            Expression.PropertyOrField(lambdaParam, "Id"),
            Expression.Constant(id, typeof(TPrimaryKey))
        );

        return Expression.Lambda<Func<TEntity, bool>>(lambdaBody, lambdaParam);
    }
}
