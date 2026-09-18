using Aviant.Core.Entities;
using Aviant.Infrastructure.Persistence.Contexts;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aviant.Tests.Persistence.Unit.Contexts;

public sealed class QueryFilterTests
{
    [Fact]
    public async Task ASoftDeletedEntityIsHiddenFromQueries()
    {
        var database = Guid.NewGuid().ToString("N");
        await using (var context = NotesContext.Create(database))
        {
            context.Notes.AddRange(new Note { Id = 1, Owner = "ada" }, new Note { Id = 2, Owner = "ada" });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            context.Notes.Remove(await context.Notes.SingleAsync(n => n.Id == 1, TestContext.Current.CancellationToken));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var fresh = NotesContext.Create(database);

        (await fresh.Notes.Select(n => n.Id).ToListAsync(TestContext.Current.CancellationToken)).Should().Equal(2);
        (await fresh.Notes.IgnoreQueryFilters().CountAsync(TestContext.Current.CancellationToken))
           .Should().Be(2, "a soft delete keeps the row");
    }

    [Fact]
    public async Task TheSoftDeleteFilterCombinesWithTheContextsOwnFilter()
    {
        var database = Guid.NewGuid().ToString("N");
        await using (var context = NotesContext.Create(database, owner: null))
        {
            context.Notes.AddRange(
                new Note { Id = 1, Owner = "ada" },
                new Note { Id = 2, Owner = "ada", IsDeleted = true },
                new Note { Id = 3, Owner = "grace" });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var scoped = NotesContext.Create(database, owner: "ada");

        (await scoped.Notes.Select(n => n.Id).ToListAsync(TestContext.Current.CancellationToken))
           .Should().Equal([1], "both the owner filter and the soft-delete filter apply");
    }

    [Fact]
    public async Task AnUnnamedFilterIsKeptAlongsideTheSoftDeleteFilter()
    {
        var database = Guid.NewGuid().ToString("N");
        await using (var context = UnnamedFilterContext.Create(database, owner: null))
        {
            context.Notes.AddRange(
                new Note { Id = 1, Owner = "ada" },
                new Note { Id = 2, Owner = "ada", IsDeleted = true },
                new Note { Id = 3, Owner = "grace" });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var scoped = UnnamedFilterContext.Create(database, owner: "ada");

        (await scoped.Notes.Select(n => n.Id).ToListAsync(TestContext.Current.CancellationToken)).Should().Equal(1);
    }

    [Fact]
    public async Task TheReadContextHidesSoftDeletedRowsToo()
    {
        var database = Guid.NewGuid().ToString("N");
        await using (var context = NotesContext.Create(database))
        {
            context.Notes.AddRange(new Note { Id = 1, Owner = "ada" }, new Note { Id = 2, Owner = "ada", IsDeleted = true });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var reader = new NotesReader(new DbContextOptionsBuilder<NotesReader>().UseInMemoryDatabase(database).Options);

        (await reader.Notes.Select(n => n.Id).ToListAsync(TestContext.Current.CancellationToken)).Should().Equal(1);
    }

    public sealed class NotesReader(DbContextOptions options) : DbContextRead(options)
    {
        public DbSet<Note> Notes => Set<Note>();
    }

    public sealed class UnnamedFilterContext(DbContextOptions options, string? owner) : DbContextWrite<UnnamedFilterContext>(options)
    {
        public DbSet<Note> Notes => Set<Note>();

        public static UnnamedFilterContext Create(string database, string? owner = null) =>
            new(new DbContextOptionsBuilder<UnnamedFilterContext>().UseInMemoryDatabase(database).Options, owner);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Note>().HasQueryFilter(n => owner == null || n.Owner == owner);

            base.OnModelCreating(modelBuilder);
        }
    }

    public sealed class Note : Entity<int>, ISoftDelete
    {
        public string Owner { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }

    public sealed class NotesContext(DbContextOptions options, string? owner) : DbContextWrite<NotesContext>(options)
    {
        public DbSet<Note> Notes => Set<Note>();

        public static NotesContext Create(string database, string? owner = null) =>
            new(new DbContextOptionsBuilder<NotesContext>().UseInMemoryDatabase(database).Options, owner);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Note>().HasQueryFilter("owner", n => owner == null || n.Owner == owner);

            base.OnModelCreating(modelBuilder);
        }
    }
}
