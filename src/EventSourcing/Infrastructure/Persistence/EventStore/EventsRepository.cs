using System.Text.Json;
using Aviant.Core.EventSourcing.Aggregates;
using Aviant.Core.EventSourcing.DomainEvents;
using Aviant.Core.EventSourcing.Persistence;
using Aviant.Core.EventSourcing.Services;
using KurrentDB.Client;

namespace Aviant.Infrastructure.EventSourcing.Persistence.EventStore;

internal sealed class EventsRepository<TAggregate, TAggregateId> : IEventsRepository<TAggregate, TAggregateId>
    where TAggregate : class, IAggregate<TAggregateId>
    where TAggregateId : class, IAggregateId
{
    private readonly KurrentDBClient _client;

    private readonly IEventSerializer _eventSerializer;

    private readonly string _streamBaseName = typeof(TAggregate).Name;

    public EventsRepository(KurrentDBClient client, IEventSerializer eventSerializer)
    {
        _client          = client;
        _eventSerializer = eventSerializer;
    }

    #region IEventsRepository<TAggregate,TAggregateId> Members

    public async Task AppendAsync(
        TAggregate        aggregate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        if (!aggregate.Events.Any())
            return;

        IDomainEvent<TAggregateId> firstEvent = aggregate.Events.First();

        // The append is atomic: every pending event is written, or none is.
        // A stale expected revision fails with WrongExpectedVersionException.
        var expectedState = 0 == firstEvent.AggregateVersion
            ? StreamState.NoStream
            : StreamState.StreamRevision((ulong)(firstEvent.AggregateVersion - 1));

        await _client.AppendToStreamAsync(
                GetStreamName(aggregate.Id),
                expectedState,
                aggregate.Events.Select(Map),
                cancellationToken: cancellationToken)
           .ConfigureAwait(false);
    }

    public async Task<TAggregate?> RehydrateAsync(
        TAggregateId      aggregateId,
        CancellationToken cancellationToken = default)
    {
        var stream = _client.ReadStreamAsync(
            Direction.Forwards,
            GetStreamName(aggregateId),
            StreamPosition.Start,
            cancellationToken: cancellationToken);

        if (await stream.ReadState.ConfigureAwait(false) == ReadState.StreamNotFound)
            return null;

        List<IDomainEvent<TAggregateId>> events = [];

        await foreach (var resolvedEvent in stream.ConfigureAwait(false))
            events.Add(Map(resolvedEvent));

        if (events.Count == 0)
            return null;

        return Aggregate<TAggregate, TAggregateId>.Create(events.OrderBy(e => e.AggregateVersion));
    }

    #endregion

    private string GetStreamName(TAggregateId aggregateId) => $"{_streamBaseName}_{aggregateId}";

    private IDomainEvent<TAggregateId> Map(ResolvedEvent resolvedEvent)
    {
        var meta = JsonSerializer.Deserialize<EventMeta>(resolvedEvent.Event.Metadata.Span);

        return _eventSerializer.Deserialize<TAggregateId>(meta.EventType, resolvedEvent.Event.Data.ToArray());
    }

    private static EventData Map(IDomainEvent<TAggregateId> @event)
    {
        var eventType = @event.GetType();

        byte[] data     = JsonSerializer.SerializeToUtf8Bytes(@event, eventType);
        byte[] metadata = JsonSerializer.SerializeToUtf8Bytes(new EventMeta { EventType = eventType.AssemblyQualifiedName! });

        return new EventData(Uuid.NewUuid(), eventType.Name, data, metadata);
    }
}

internal struct EventMeta : IEquatable<EventMeta>
{
    public string EventType { get; set; }

    /// <inheritdoc />
    public bool Equals(EventMeta other) => EventType == other.EventType;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is EventMeta other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => EventType.GetHashCode();

    public static bool operator ==(EventMeta left, EventMeta right) => left.Equals(right);

    public static bool operator !=(EventMeta left, EventMeta right) => !left.Equals(right);
}
