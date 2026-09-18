using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Aviant.Core.EventSourcing.Aggregates;
using Aviant.Core.EventSourcing.EventBus;
using Microsoft.Extensions.Logging;

namespace Aviant.Infrastructure.EventSourcing.Transport.Kafka;

internal sealed partial class EventProducer<TAggregate, TAggregateId> : IEventProducer<TAggregate, TAggregateId>
    where TAggregate : IAggregate<TAggregateId>
    where TAggregateId : class, IAggregateId
{
    private readonly ILogger _logger;

    private readonly string _topicName;

    private IProducer<TAggregateId, string> _producer;

    public EventProducer(
        string                                          topicName,
        string                                          kafkaConnString,
        ILogger<EventProducer<TAggregate, TAggregateId>> logger)
    {
        _logger = logger;

        if (string.IsNullOrWhiteSpace(topicName))
            throw new ArgumentNullException(nameof(topicName));

        if (string.IsNullOrWhiteSpace(kafkaConnString))
            throw new ArgumentNullException(nameof(kafkaConnString));

        var aggregateType = typeof(TAggregate);

        _topicName = $"{topicName}-{aggregateType.Name}";

        ProducerConfig producerConfig = new()
        {
            BootstrapServers    = kafkaConnString,
            BrokerAddressFamily = BrokerAddressFamily.V4
        };
        ProducerBuilder<TAggregateId, string> producerBuilder = new(producerConfig);
        producerBuilder.SetKeySerializer(new KeySerializer<TAggregateId>());
        _producer = producerBuilder.Build();
    }

    #region IEventProducer<TAggregate,TAggregateId> Members

    public void Dispose()
    {
        _producer.Dispose();
        _producer = null!;
    }

    public Task DispatchAsync(
        TAggregate        aggregate,
        CancellationToken cancellationToken = default)
    {
        if (aggregate is null)
            throw new ArgumentNullException(nameof(aggregate));

        return !aggregate.Events.Any()
            ? Task.CompletedTask
            : DispatchEventsAsync(aggregate, cancellationToken);
    }

    #endregion

    private async Task DispatchEventsAsync(
        TAggregate        aggregate,
        CancellationToken cancellationToken = default)
    {
        LogPublishing(_logger, aggregate.Events.Count, aggregate.Id);

        foreach (Message<TAggregateId, string>? message in
                 from @event in aggregate.Events
                 let eventType = @event.GetType()
                 let serialized = JsonSerializer.Serialize(@event, eventType)
                 let headers = new Headers
                 {
                     { "aggregate", Encoding.UTF8.GetBytes(@event.AggregateId.ToString()!) },
                     { "type", Encoding.UTF8.GetBytes(eventType.AssemblyQualifiedName!) }
                 }
                 select new Message<TAggregateId, string>
                 {
                     Key     = @event.AggregateId,
                     Value   = serialized,
                     Headers = headers
                 })
            await _producer.ProduceAsync(_topicName, message, cancellationToken)
               .ConfigureAwait(false);

        aggregate.ClearEvents();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Publishing {EventsCount} events for {AggregateId}")]
    private static partial void LogPublishing(ILogger logger, int eventsCount, TAggregateId aggregateId);
}
