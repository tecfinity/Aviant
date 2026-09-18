using System.Net;
using Aviant.Application.EventSourcing.Services;
using Aviant.Core.EventSourcing.Aggregates;
using Aviant.Core.EventSourcing.DomainEvents;
using Aviant.Core.EventSourcing.Persistence;
using Aviant.Core.EventSourcing.Services;
using Aviant.Infrastructure.EventSourcing.Persistence.EventStore;
using AwesomeAssertions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using KurrentDB.Client;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace Aviant.Tests.EventSourcing.Integration;

/// <summary>
///     The events repository against a real KurrentDB server, over gRPC.
/// </summary>
public sealed class KurrentDbEventsRepositoryTests : IAsyncLifetime
{
    private readonly IContainer _kurrentDb = new ContainerBuilder("kurrentplatform/kurrentdb:26.1.2")
       .WithEnvironment("KURRENTDB_CLUSTER_SIZE", "1")
       .WithEnvironment("KURRENTDB_RUN_PROJECTIONS", "None")
       .WithEnvironment("KURRENTDB_INSECURE", "true")
       .WithEnvironment("KURRENTDB_MEM_DB", "true")
       .WithPortBinding(2113, assignRandomHostPort: true)
       .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(2113).ForPath("/health/live").ForStatusCode(HttpStatusCode.NoContent)))
       .Build();

    private ServiceProvider _services = null!;

    public async ValueTask InitializeAsync()
    {
        await _kurrentDb.StartAsync(TestContext.Current.CancellationToken);

        var services = new ServiceCollection();
        services.AddKurrentDb($"kurrentdb://admin:changeit@{_kurrentDb.Hostname}:{_kurrentDb.GetMappedPublicPort(2113)}?tls=false");
        services.AddSingleton<IEventSerializer>(new JsonEventSerializer([typeof(Profile).Assembly]));
        services.AddEventsRepository<Profile, ProfileId>();
        _services = services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _kurrentDb.DisposeAsync();
    }

    [Fact]
    public async Task AnAggregateIsRebuiltFromTheEventsItAppended()
    {
        var repository = Repository();
        var profile    = Profile.Open(new ProfileId(Guid.NewGuid()), "Ada");
        profile.Rename("Ada Lovelace");

        await repository.AppendAsync(profile, TestContext.Current.CancellationToken);
        var rebuilt = await repository.RehydrateAsync(profile.Id, TestContext.Current.CancellationToken);

        rebuilt.Should().NotBeNull();
        rebuilt!.Name.Should().Be("Ada Lovelace");
        rebuilt.Version.Should().Be(2);
    }

    [Fact]
    public async Task LaterEventsAppendToTheSameStream()
    {
        var repository = Repository();
        var id         = new ProfileId(Guid.NewGuid());
        await repository.AppendAsync(Profile.Open(id, "Ada"), TestContext.Current.CancellationToken);

        var loaded = (await repository.RehydrateAsync(id, TestContext.Current.CancellationToken))!;
        loaded.Rename("Countess of Lovelace");
        await repository.AppendAsync(loaded, TestContext.Current.CancellationToken);

        var rebuilt = await repository.RehydrateAsync(id, TestContext.Current.CancellationToken);
        rebuilt!.Name.Should().Be("Countess of Lovelace");
        rebuilt.Version.Should().Be(2);
    }

    [Fact]
    public async Task AConcurrentWriteWithAStaleVersionIsRejected()
    {
        var repository = Repository();
        var id         = new ProfileId(Guid.NewGuid());
        await repository.AppendAsync(Profile.Open(id, "Ada"), TestContext.Current.CancellationToken);
        var first  = (await repository.RehydrateAsync(id, TestContext.Current.CancellationToken))!;
        var second = (await repository.RehydrateAsync(id, TestContext.Current.CancellationToken))!;

        first.Rename("One");
        await repository.AppendAsync(first, TestContext.Current.CancellationToken);
        second.Rename("Two");
        var act = () => repository.AppendAsync(second, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<WrongExpectedVersionException>("the stream moved on since this copy was loaded");
    }

    [Fact]
    public async Task AnAggregateThatWasNeverSavedIsNotFound() =>
        (await Repository().RehydrateAsync(new ProfileId(Guid.NewGuid()), TestContext.Current.CancellationToken))
           .Should().BeNull();

    private IEventsRepository<Profile, ProfileId> Repository() =>
        _services.GetRequiredService<IEventsRepository<Profile, ProfileId>>();

    public sealed class ProfileId : AggregateId<Guid>
    {
        [JsonConstructor]
        public ProfileId(Guid key)
            : base(key)
        { }
    }

    public sealed class Profile : Aggregate<Profile, ProfileId>
    {
        private Profile()
        { }

        private Profile(ProfileId id)
            : base(id)
        { }

        public string Name { get; private set; } = string.Empty;

        public static Profile Open(ProfileId id, string name)
        {
            var profile = new Profile(id);
            profile.AddEvent(new ProfileRenamed(profile, name));

            return profile;
        }

        public void Rename(string name) => AddEvent(new ProfileRenamed(this, name));

        protected override void Apply(IDomainEvent<ProfileId> @event)
        {
            if (@event is ProfileRenamed renamed)
            {
                Id   = renamed.AggregateId;
                Name = renamed.Name;
            }
        }
    }

    public sealed record ProfileRenamed : DomainEvent<Profile, ProfileId>
    {
        #pragma warning disable 8618
        private ProfileRenamed()
        { }
        #pragma warning restore 8618

        public ProfileRenamed(Profile profile, string name)
            : base(profile) => Name = name;

        public string Name { get; private set; }
    }
}
