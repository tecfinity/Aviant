using System.Text;
using Confluent.Kafka;
using Aviant.Core.EventSourcing.Aggregates;
using Aviant.Core.EventSourcing.DomainEvents;
using Aviant.Core.EventSourcing.EventBus;
using Aviant.Core.EventSourcing.Services;
using Microsoft.Extensions.Logging;

namespace Aviant.Infrastructure.EventSourcing.Transport.Kafka;

public sealed partial class EventConsumer<TAggregate, TAggregateId, TDeserializer>
    : IDisposable, IEventConsumer<TAggregate, TAggregateId, TDeserializer>
    where TAggregate : IAggregate<TAggregateId>
    where TAggregateId : class, IAggregateId
    where TDeserializer : class, IDeserializer<TAggregateId>, new()
{
    #region Delegates

    public delegate void ConsumerStoppedHandler(object sender);

    #endregion

    private readonly IEventSerializer _eventSerializer;

    private readonly ILogger _logger;

    private IConsumer<TAggregateId, string> _eventConsumer;

    #pragma warning disable 8618
    public EventConsumer(
        IEventSerializer                                                eventSerializer,
        EventsConsumerConfig                                            config,
        ILogger<EventConsumer<TAggregate, TAggregateId, TDeserializer>> logger)
    {
        _eventSerializer = eventSerializer;
        _logger          = logger;

        var aggregateType = typeof(TAggregate);

        ConsumerConfig consumerConfig = new()
        {
            GroupId             = config.ConsumerGroup,
            BootstrapServers    = config.KafkaConnectionString,
            AutoOffsetReset     = AutoOffsetReset.Earliest,
            EnablePartitionEof  = true,
            BrokerAddressFamily = BrokerAddressFamily.V4
        };

        ConsumerBuilder<TAggregateId, string> consumerBuilder = new(consumerConfig);
        consumerBuilder.SetKeyDeserializer(KeyDeserializerFactory.Create<TDeserializer, TAggregateId>());

        _eventConsumer = consumerBuilder.Build();

        var topicName = $"{config.TopicName}-{aggregateType.Name}";
        _eventConsumer.Subscribe(topicName);
    }
    #pragma warning restore 8618

    #region IDisposable Members

    public void Dispose()
    {
        _eventConsumer.Dispose();
        _eventConsumer = null!;
    }

    #endregion

    #region IEventConsumer<TAggregate,TAggregateId,TDeserializer> Members

    // ReSharper disable once CognitiveComplexity
    public Task ConsumeAsync(CancellationToken cancellationToken)
    {
        return Task.Run(
            async () =>
            {
                var topics = string.Join(",", _eventConsumer.Subscription);

                LogStarted(_logger, _eventConsumer.Name, topics);

                while (!cancellationToken.IsCancellationRequested)
                    try
                    {
                        ConsumeResult<TAggregateId, string> cr = _eventConsumer.Consume(cancellationToken);

                        if (cr.IsPartitionEOF)
                            continue;

                        var messageTypeHeader = cr.Message.Headers.First(h => h.Key == "type");
                        var eventType         = Encoding.UTF8.GetString(messageTypeHeader.GetValueBytes());

                        IDomainEvent<TAggregateId> @event =
                            _eventSerializer.Deserialize<TAggregateId>(eventType, cr.Message.Value);

                        await OnEventReceivedAsync(@event, cancellationToken)
                           .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException ex)
                    {
                        LogStopped(_logger, ex, _eventConsumer.Name, topics);
                        OnConsumerStopped();
                    }
                    catch (Exception ex)
                    {
                        LogConsumeFailed(_logger, ex, _eventConsumer.Name, topics);
                        OnExceptionThrown(ex);
                    }
            },
            cancellationToken);
    }

    public event EventReceivedHandlerAsync<TAggregateId> EventReceived;

    #endregion

    private Task OnEventReceivedAsync(
        IDomainEvent<TAggregateId> e,
        CancellationToken          cancellationToken)
    {
        EventReceivedHandlerAsync<TAggregateId> handlerAsync = EventReceived;

        return handlerAsync.Invoke(this, e, cancellationToken)
            ?? throw new NullReferenceException(
                   typeof(EventConsumer<TAggregate, TAggregateId, TDeserializer>).FullName);
    }

    public event ExceptionThrownHandler ExceptionThrown;

    private void OnExceptionThrown(Exception e)
    {
        var handler = ExceptionThrown;
        handler.Invoke(this, e);
    }

    public event ConsumerStoppedHandler ConsumerStopped;

    private void OnConsumerStopped()
    {
        var handler = ConsumerStopped;
        handler.Invoke(this);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Started Kafka consumer {ConsumerName} on {ConsumerTopic}")]
    private static partial void LogStarted(ILogger logger, string consumerName, string consumerTopic);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Kafka consumer {ConsumerName} on {ConsumerTopic} was stopped")]
    private static partial void LogStopped(ILogger logger, Exception exception, string consumerName, string consumerTopic);

    [LoggerMessage(Level = LogLevel.Error, Message = "Kafka consumer {ConsumerName} on {ConsumerTopic} failed to handle a message")]
    private static partial void LogConsumeFailed(ILogger logger, Exception exception, string consumerName, string consumerTopic);
}
