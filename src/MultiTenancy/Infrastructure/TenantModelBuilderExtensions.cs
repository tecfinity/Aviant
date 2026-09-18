using System.Linq.Expressions;
using System.Reflection;
using Aviant.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Aviant.Infrastructure.MultiTenancy;

public static class TenantModelBuilderExtensions
{
    /// <summary>
    ///     The name of the tenant query filter.
    /// </summary>
    public const string TenantFilterName = "Aviant.Tenant";

    /// <summary>
    ///     Scopes every <see cref="ITenantOwned" /> entity to the current tenant with the named filter
    ///     <see cref="TenantFilterName" />.
    /// </summary>
    /// <param name="modelBuilder">The context's model builder.</param>
    /// <param name="context">The context being configured, usually <c>this</c>.</param>
    /// <param name="filter">
    ///     A member of <paramref name="context" /> that returns its <see cref="TenantFilter" />, e.g.
    ///     <c>() => TenantFilter</c>. It must be read through the context, so each query sees that context's tenant.
    /// </param>
    /// <param name="includeRowsWithoutTenant">
    ///     Also return rows whose tenant is <see langword="null" />, such as rows written before tenancy existed.
    /// </param>
    public static ModelBuilder UseTenantFilter(
        this ModelBuilder             modelBuilder,
        DbContext                     context,
        Expression<Func<TenantFilter>> filter,
        bool                          includeRowsWithoutTenant = false)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(filter);

        if (filter.Body is not MemberExpression { Member: PropertyInfo or FieldInfo } member
         || !member.Member.DeclaringType!.IsInstanceOfType(context))
            throw new ArgumentException(
                "The filter must be a property or field of the context, e.g. () => TenantFilter.",
                nameof(filter));

        // this.<member>, rooted at the context so EF Core re-reads it for every query.
        var tenantFilter = Expression.MakeMemberAccess(Expression.Constant(context), member.Member);
        var unfiltered   = Expression.Property(tenantFilter, nameof(TenantFilter.Unfiltered));
        var current      = Expression.Property(tenantFilter, nameof(TenantFilter.CurrentTenantId));

        foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes().ToList())
        {
            if (entity.BaseType is not null || entity.IsOwned() || !typeof(ITenantOwned).IsAssignableFrom(entity.ClrType))
                continue;

            var row   = Expression.Parameter(entity.ClrType, "row");
            var owner = Expression.Call(
                typeof(EF),
                nameof(EF.Property),
                [typeof(Guid?)],
                row,
                Expression.Constant(nameof(ITenantOwned.TenantId)));

            Expression visible = Expression.OrElse(unfiltered, Expression.Equal(owner, current));

            if (includeRowsWithoutTenant)
                visible = Expression.OrElse(visible, Expression.Equal(owner, Expression.Constant(null, typeof(Guid?))));

            modelBuilder.Entity(entity.ClrType).HasQueryFilter(TenantFilterName, Expression.Lambda(visible, row));
        }

        return modelBuilder;
    }
}
