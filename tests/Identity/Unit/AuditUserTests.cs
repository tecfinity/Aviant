using Aviant.Application.Identity;
using Aviant.Core.Identity.Entities;
using Aviant.Infrastructure.Identity.Persistence.Contexts;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aviant.Tests.Identity.Unit;

/// <summary>
///     Audited entities are stamped with the current user of the scope that created the context,
///     with no HttpContext or static locator involved.
/// </summary>
public sealed class AuditUserTests
{
    [Fact]
    public async Task CreatedByIsTheCurrentUserOfTheContextsScope()
    {
        var user = Guid.NewGuid();
        await using var provider = Services(currentUser: user);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<NoteContext>();
        var note = new Note { Id = 1 };

        context.Notes.Add(note);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        note.CreatedBy.Should().Be(user);
    }

    [Fact]
    public async Task WithoutACurrentUserTheEntityIsStampedWithEmpty()
    {
        await using var provider = Services(currentUser: null);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<NoteContext>();
        var note = new Note { Id = 1 };

        context.Notes.Add(note);
        var act = () => context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync("background work has no user and must still be able to save");
        note.CreatedBy.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task ASoftDeleteRecordsWhoDeletedTheEntity()
    {
        var user = Guid.NewGuid();
        await using var provider = Services(currentUser: user);
        await using (var scope = provider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<NoteContext>();
            context.Tasks.Add(new TaskItem { Id = 1 });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            context.Tasks.Remove(await context.Tasks.SingleAsync(TestContext.Current.CancellationToken));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var fresh = provider.CreateAsyncScope();
        var stored = await fresh.ServiceProvider.GetRequiredService<NoteContext>().Tasks
           .IgnoreQueryFilters().SingleAsync(TestContext.Current.CancellationToken);

        stored.IsDeleted.Should().BeTrue();
        stored.DeletedBy.Should().Be(user);
        stored.Deleted.Should().NotBeNull();
    }

    private static ServiceProvider Services(Guid? currentUser)
    {
        var services = new ServiceCollection();
        var database = Guid.NewGuid().ToString("N");
        services.AddDbContext<NoteContext>(options => options.UseInMemoryDatabase(database));

        if (currentUser is { } id)
            services.AddScoped<ICurrentUserService>(_ => new FixedUser(id));

        return services.BuildServiceProvider();
    }

    public sealed class Note : ICreationAudited
    {
        public int Id { get; set; }

        public DateTimeOffset Created { get; set; }

        public Guid CreatedBy { get; set; }
    }

    public sealed class TaskItem : IDeletionAudited, Aviant.Core.Entities.ISoftDelete
    {
        public int Id { get; set; }

        public DateTimeOffset? Deleted { get; set; }

        public Guid? DeletedBy { get; set; }

        public bool IsDeleted { get; set; }
    }

    public sealed class NoteContext(DbContextOptions<NoteContext> options) : DbContextWrite<NoteContext>(options)
    {
        public DbSet<Note> Notes => Set<Note>();

        public DbSet<TaskItem> Tasks => Set<TaskItem>();
    }

    private sealed record FixedUser(Guid UserId) : ICurrentUserService;
}
