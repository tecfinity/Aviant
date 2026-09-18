using Aviant.Core.Entities;
using Aviant.Core.Timing;
using Aviant.Infrastructure.Persistence.Contexts;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aviant.Tests.Persistence.Unit.Contexts;

[Collection("Clock")]
public sealed class AuditingTests
{
    private static readonly DateTimeOffset Instant = new(2030, 5, 6, 7, 8, 9, TimeSpan.Zero);

    [Fact]
    public async Task ANewEntityIsStampedWithTheClocksTime()
    {
        using var _ = FreezeTime();
        await using var context = LedgerContext.Create();
        var entry = new Entry { Id = 1 };

        context.Entries.Add(entry);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        entry.Created.Should().Be(Instant);
        entry.Updated.Should().BeNull();
    }

    [Fact]
    public async Task AChangedEntityIsStampedWithTheUpdateTime()
    {
        using var _ = FreezeTime();
        await using var context = LedgerContext.Create();
        var entry = new Entry { Id = 1 };
        context.Entries.Add(entry);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        entry.Memo = "changed";
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        entry.Updated.Should().Be(Instant);
    }

    [Fact]
    public void ASynchronousSaveIsAuditedToo()
    {
        using var _ = FreezeTime();
        using var context = LedgerContext.Create();
        var entry = new Entry { Id = 1 };

        context.Entries.Add(entry);
        context.SaveChanges();

        entry.Created.Should().Be(Instant);
    }

    [Fact]
    public async Task ASoftDeleteRecordsWhenItHappened()
    {
        using var _ = FreezeTime();
        var database = Guid.NewGuid().ToString("N");
        await using (var context = LedgerContext.Create(database))
        {
            context.Entries.Add(new Entry { Id = 1 });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            context.Entries.Remove(await context.Entries.SingleAsync(TestContext.Current.CancellationToken));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var fresh = LedgerContext.Create(database);
        var deleted = await fresh.Entries.IgnoreQueryFilters().SingleAsync(TestContext.Current.CancellationToken);

        deleted.IsDeleted.Should().BeTrue();
        deleted.Deleted.Should().Be(Instant);
    }

    [Fact]
    public async Task AReadOnlyEntityCannotBeChanged()
    {
        await using var context = LedgerContext.Create();
        var sealedEntry = new SealedEntry { Id = 1, Memo = "original" };
        context.SealedEntries.Add(sealedEntry);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        sealedEntry.Memo = "tampered";
        var act = () => context.SaveChangesAsync(TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<DbUpdateException>()).Which.Message.Should().Contain(nameof(SealedEntry));
    }

    private static IDisposable FreezeTime()
    {
        var previous = Clock.TimeProvider;
        Clock.TimeProvider = new FixedTime(Instant);

        return new Restore(() => Clock.TimeProvider = previous);
    }

    public sealed class Entry : Entity<int>, IHasCreationTime, IHasUpdatedTime, IHasDeletionTime, ISoftDelete
    {
        public string Memo { get; set; } = string.Empty;

        public DateTimeOffset Created { get; set; }

        public DateTimeOffset? Updated { get; set; }

        public DateTimeOffset? Deleted { get; set; }

        public bool IsDeleted { get; set; }
    }

    public sealed class SealedEntry : Entity<int>, IReadOnly
    {
        public string Memo { get; set; } = string.Empty;
    }

    public sealed class LedgerContext(DbContextOptions options) : DbContextWrite<LedgerContext>(options)
    {
        public DbSet<Entry> Entries => Set<Entry>();

        public DbSet<SealedEntry> SealedEntries => Set<SealedEntry>();

        public static LedgerContext Create(string? database = null) =>
            new(new DbContextOptionsBuilder<LedgerContext>().UseInMemoryDatabase(database ?? Guid.NewGuid().ToString("N")).Options);
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
