using Aviant.Core.Entities;
using Aviant.Core.Exceptions;
using Aviant.Infrastructure.Persistence.Contexts;
using Aviant.Infrastructure.Persistence.Repository;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aviant.Tests.Persistence.Unit.Repository;

public sealed class RepositoryTests
{
    [Fact]
    public async Task InsertRefusesAnEntityThatReportsItselfInvalid()
    {
        await using var context = WriteContext.Create();
        var repository = new Books(context);

        var act = () => repository.InsertAsync(new Book { Id = 1, Title = "" }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DomainRuleException>();
        context.ChangeTracker.Entries().Should().BeEmpty("an invalid entity must not be tracked for saving");
    }

    [Fact]
    public async Task InsertAddsAValidEntity()
    {
        await using var context = WriteContext.Create();
        var repository = new Books(context);

        await repository.InsertAsync(new Book { Id = 1, Title = "Dune" }, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        (await context.Books.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    [Fact]
    public async Task UpdatingATrackedEntityOnlyMarksWhatChanged()
    {
        await using var context = WriteContext.Create();
        context.Books.Add(new Book { Id = 1, Title = "Dune", Author = "Herbert" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var book = await context.Books.SingleAsync(TestContext.Current.CancellationToken);
        book.Title = "Dune Messiah";

        await new Books(context).UpdateAsync(book, TestContext.Current.CancellationToken);

        var entry = context.Entry(book);
        entry.Property(b => b.Title).IsModified.Should().BeTrue();
        entry.Property(b => b.Author).IsModified.Should().BeFalse("only the changed column is written");
    }

    [Fact]
    public async Task UpdatingADetachedEntityAttachesIt()
    {
        await using var context = WriteContext.Create();
        context.Books.Add(new Book { Id = 1, Title = "Dune" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        await new Books(context).UpdateAsync(new Book { Id = 1, Title = "Children of Dune" }, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        (await context.Books.SingleAsync(TestContext.Current.CancellationToken)).Title.Should().Be("Children of Dune");
    }

    [Fact]
    public async Task AsyncReadsHonourCancellation()
    {
        await using var context = ReadContext.Create();
        var repository = new ReadBooks(context);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var act = async () => await repository.GetAllListAsync(cancelled.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetAsyncStillReportsAMissingEntity()
    {
        await using var context = ReadContext.Create();

        var act = async () => await new ReadBooks(context).GetAsync(42, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    public sealed class Book : Entity<int>
    {
        public string Title { get; set; } = string.Empty;

        public string Author { get; set; } = string.Empty;

        public override Task<bool> ValidateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(!string.IsNullOrWhiteSpace(Title));
    }

    public sealed class WriteContext(DbContextOptions options) : DbContextWrite<WriteContext>(options)
    {
        public DbSet<Book> Books => Set<Book>();

        public static WriteContext Create() =>
            new(new DbContextOptionsBuilder<WriteContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
    }

    public sealed class ReadContext(DbContextOptions options) : DbContextRead(options)
    {
        public DbSet<Book> Books => Set<Book>();

        public static ReadContext Create() =>
            new(new DbContextOptionsBuilder<ReadContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
    }

    private sealed class Books(WriteContext context) : RepositoryWrite<WriteContext, Book, int>(context);

    private sealed class ReadBooks(ReadContext context) : RepositoryRead<ReadContext, Book, int>(context);
}
