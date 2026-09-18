using Aviant.Core.Entities;
using Aviant.Infrastructure.Persistence.Contexts;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aviant.Tests.Persistence.Unit.Contexts;

public sealed class AuditChangeTrackerTests
{
    [Fact]
    public async Task SavingWhileAnotherAuditedEntityIsUnchangedSucceeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = NoteContext.Create();
        context.Notes.AddRange(new Note { Id = 1, Text = "first" }, new Note { Id = 2, Text = "second" });
        await context.SaveChangesAsync(cancellationToken);

        var second = await context.Notes.SingleAsync(note => note.Id == 2, cancellationToken);
        second.Text = "second, edited";

        var act = () => context.SaveChangesAsync(cancellationToken);

        await act.Should().NotThrowAsync("an audited entity that was loaded but not modified is simply not audited");
        second.Updated.Should().NotBeNull();
        (await context.Notes.SingleAsync(note => note.Id == 1, cancellationToken)).Updated.Should().BeNull();
    }

    [Fact]
    public async Task CreationTimeIsStampedOnInsert()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = NoteContext.Create();
        var note = new Note { Id = 3, Text = "third" };

        context.Notes.Add(note);
        await context.SaveChangesAsync(cancellationToken);

        note.Created.Should().NotBe(default);
    }

    public sealed class Note : IHasCreationTime, IHasUpdatedTime
    {
        public int Id { get; set; }

        public string Text { get; set; } = string.Empty;

        public DateTimeOffset Created { get; set; }

        public DateTimeOffset? Updated { get; set; }
    }

    public sealed class NoteContext(DbContextOptions options) : DbContextWrite<NoteContext>(options)
    {
        public DbSet<Note> Notes => Set<Note>();

        public static NoteContext Create() => new(
            new DbContextOptionsBuilder<NoteContext>()
               .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
               .Options);
    }
}
