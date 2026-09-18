using Aviant.Core.EventSourcing.Aggregates;
using Aviant.Core.EventSourcing.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aviant.Infrastructure.EventSourcing.Transport.Kafka;

public static class KafkaExtensionRegistry
{
    public static IServiceCollection AddKafkaEventProducer<TAggregate, TAggregateId>(
        this IServiceCollection services,
        EventsProducerConfig     configuration)
        where TAggregate : class, IAggregate<TAggregateId>
        where TAggregateId : class, IAggregateId
    {
        return services.AddSingleton<IEventProducer<TAggregate, TAggregateId>>(
            ctx => new EventProducer<TAggregate, TAggregateId>(
                configuration.TopicName,
                configuration.KafkaConnectionString,
                ctx.GetRequiredService<ILogger<EventProducer<TAggregate, TAggregateId>>>()));
    }
}
