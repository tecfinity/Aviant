using Aviant.Infrastructure.Persistence.Conventions;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Aviant.Tests.Persistence.Unit.Contexts;

public sealed class ConventionTests
{
    [Fact]
    public async Task ANewChildOfALoadedParentIsInsertedNotUpdated()
    {
        var database = Guid.NewGuid().ToString("N");
        var parentId = Guid.NewGuid();
        await using (var context = ShelfContext.Create(database))
        {
            context.Shelves.Add(new Shelf(parentId));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = ShelfContext.Create(database))
        {
            var shelf = await context.Shelves.Include(s => s.Books).SingleAsync(TestContext.Current.CancellationToken);
            shelf.Books.Add(new Book(Guid.NewGuid()));
            context.ChangeTracker.DetectChanges();

            context.Entry(shelf.Books[0]).State.Should().Be(EntityState.Added);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var fresh = ShelfContext.Create(database);
        (await fresh.Set<Book>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    [Fact]
    public void GuidKeysAreNeverStoreGenerated()
    {
        using var context = ShelfContext.Create(Guid.NewGuid().ToString("N"));

        context.Model.FindEntityType(typeof(Shelf))!.FindPrimaryKey()!.Properties[0].ValueGenerated
           .Should().Be(ValueGenerated.Never);
    }

    [Fact]
    public void InstantsAreStoredInUtc()
    {
        using var context = ShelfContext.Create(Guid.NewGuid().ToString("N"));
        var converter = context.Model.FindEntityType(typeof(Shelf))!.FindProperty(nameof(Shelf.OpenedAt))!.GetValueConverter()!;
        var athens = new DateTimeOffset(2030, 7, 1, 12, 0, 0, TimeSpan.FromHours(3));

        var stored = (DateTimeOffset)converter.ConvertToProvider(athens)!;

        stored.Offset.Should().Be(TimeSpan.Zero);
        stored.Should().Be(athens, "the instant is unchanged");
    }

    public sealed class Shelf(Guid id)
    {
        public Guid Id { get; private set; } = id;

        public DateTimeOffset OpenedAt { get; set; } = DateTimeOffset.UtcNow;

        public List<Book> Books { get; } = [];
    }

    public sealed class Book(Guid id)
    {
        public Guid Id { get; private set; } = id;
    }

    public sealed class ShelfContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Shelf> Shelves => Set<Shelf>();

        public static ShelfContext Create(string database) =>
            new(new DbContextOptionsBuilder<ShelfContext>().UseInMemoryDatabase(database).Options);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Shelf>().HasMany(s => s.Books).WithOne();
            modelBuilder.UseDomainAssignedKeys().UseUtcTimestamps();
        }
    }
}
