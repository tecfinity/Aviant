using System.Linq.Expressions;
using Aviant.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Aviant.Infrastructure.Persistence.Conventions;

/// <summary>
///     Model conventions for domain-driven entities. Call them at the end of <c>OnModelCreating</c>, after the
///     entity configurations, so a configuration that says otherwise still wins.
/// </summary>
public static class ModelBuilderConventions
{
    /// <summary>
    ///     The name of the query filter that hides soft-deleted rows. Pass it to
    ///     <c>IgnoreQueryFilters([ModelBuilderConventions.SoftDeleteFilter])</c> to include them.
    /// </summary>
    public const string SoftDeleteFilter = "Aviant.SoftDelete";

    private static readonly ValueConverter<DateTimeOffset, DateTimeOffset> ToUtc =
        new(value => value.ToUniversalTime(), value => value);

    private static readonly ValueConverter<DateTimeOffset?, DateTimeOffset?> NullableToUtc =
        new(value => value.HasValue ? value.Value.ToUniversalTime() : null, value => value);

    /// <summary>
    ///     Hides soft-deleted rows of every <see cref="ISoftDelete" /> entity without replacing the context's own
    ///     filters.
    /// </summary>
    /// <remarks>
    ///     The condition is added as the named filter <see cref="SoftDeleteFilter" />, which combines with other
    ///     named filters. EF Core does not allow named and anonymous filters on one entity, so when the entity
    ///     already has an anonymous filter the condition is ANDed into it instead; <c>IgnoreQueryFilters()</c> then
    ///     lifts both.
    /// </remarks>
    public static ModelBuilder UseSoftDeleteFilter(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes().ToList())
        {
            // Filters belong to the root of a hierarchy; owned types follow their owner.
            if (entity.BaseType is not null
             || entity.IsOwned()
             || !typeof(ISoftDelete).IsAssignableFrom(entity.ClrType))
                continue;

            // EF Core cannot mix an anonymous filter with named ones. An entity that already has an anonymous
            // filter gets the soft-delete condition ANDed into it; any other gets a named filter.
            var anonymous = entity.GetDeclaredQueryFilters().FirstOrDefault(filter => filter.IsAnonymous)?.Expression;

            if (anonymous is not null)
            {
                var row = anonymous.Parameters[0];

                modelBuilder.Entity(entity.ClrType)
                   .HasQueryFilter(Expression.Lambda(Expression.AndAlso(anonymous.Body, NotDeleted(row)), row));
            }
            else
            {
                var row = Expression.Parameter(entity.ClrType, "row");

                modelBuilder.Entity(entity.ClrType)
                   .HasQueryFilter(SoftDeleteFilter, Expression.Lambda(NotDeleted(row), row));
            }
        }

        return modelBuilder;
    }

    private static UnaryExpression NotDeleted(ParameterExpression row) =>
        Expression.Not(
            Expression.Call(
                typeof(EF),
                nameof(EF.Property),
                [typeof(bool)],
                row,
                Expression.Constant(nameof(ISoftDelete.IsDeleted))));

    /// <summary>
    ///     Declares that single-column <see cref="Guid" /> keys are assigned by the domain, never by the store.
    /// </summary>
    /// <remarks>
    ///     EF Core assumes a Guid key is generated on add. When change tracking finds an untracked entity under a
    ///     tracked one, it takes a key that is already set to mean the row exists, and saves a new child of a
    ///     loaded parent as an UPDATE that matches nothing: a <c>DbUpdateConcurrencyException</c> that has
    ///     nothing to do with concurrency. Entities that receive their id in the constructor need this.
    /// </remarks>
    public static ModelBuilder UseDomainAssignedKeys(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes())
        {
            IMutableKey? key = entity.FindPrimaryKey();

            if (key is { Properties: [{ } property] } && property.ClrType == typeof(Guid))
                property.ValueGenerated = ValueGenerated.Never;
        }

        return modelBuilder;
    }

    /// <summary>
    ///     Stores every <see cref="DateTimeOffset" /> as UTC.
    /// </summary>
    /// <remarks>
    ///     PostgreSQL (Npgsql) refuses a <see cref="DateTimeOffset" /> whose offset is not zero, and every instant
    ///     built from somebody's local time has one. The instant is unchanged. Properties that already have a
    ///     converter are left alone.
    /// </remarks>
    public static ModelBuilder UseUtcTimestamps(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes())
            foreach (IMutableProperty property in entity.GetProperties())
            {
                if (property.GetValueConverter() is not null)
                    continue;

                if (property.ClrType == typeof(DateTimeOffset))
                    property.SetValueConverter(ToUtc);
                else if (property.ClrType == typeof(DateTimeOffset?))
                    property.SetValueConverter(NullableToUtc);
            }

        return modelBuilder;
    }
}
