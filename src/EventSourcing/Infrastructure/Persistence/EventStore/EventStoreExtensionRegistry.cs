using Aviant.Application.EventSourcing.Services;
using Aviant.Core.EventSourcing.Aggregates;
using Aviant.Core.EventSourcing.EventBus;
using Aviant.Core.EventSourcing.Persistence;
using Aviant.Core.EventSourcing.Services;
using KurrentDB.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Aviant.Infrastructure.EventSourcing.Persistence.EventStore;

public static class EventStoreExtensionRegistry
{
    /// <summary>
    ///     Registers one <see cref="KurrentDBClient" /> for the application. The client is thread-safe,
    ///     multiplexes over a single gRPC channel and reconnects on its own.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">A KurrentDB connection string, e.g. <c>kurrentdb://admin:changeit@localhost:2113?tls=false</c>.</param>
    public static IServiceCollection AddKurrentDb(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var settings = KurrentDBClientSettings.Create(connectionString);

        return services.AddSingleton(_ => new KurrentDBClient(settings));
    }

    public static IServiceCollection AddEventsRepository<TAggregate, TAggregateId>(this IServiceCollection services)
        where TAggregate : class, IAggregate<TAggregateId>
        where TAggregateId : class, IAggregateId
    {
        return services.AddSingleton<IEventsRepository<TAggregate, TAggregateId>>(
            ctx =>
            {
                var client            = ctx.GetRequiredService<KurrentDBClient>();
                var eventDeserializer = ctx.GetRequiredService<IEventSerializer>();

                return new EventsRepository<TAggregate, TAggregateId>(client, eventDeserializer);
            });
    }

    public static IServiceCollection AddEventsService<TAggregate, TAggregateId>(this IServiceCollection services)
        where TAggregate : class, IAggregate<TAggregateId>
        where TAggregateId : class, IAggregateId
    {
        return services.AddSingleton<IEventsService<TAggregate, TAggregateId>>(
            ctx =>
            {
                var eventsProducer = ctx.GetRequiredService<IEventProducer<TAggregate, TAggregateId>>();
                var eventsRepo     = ctx.GetRequiredService<IEventsRepository<TAggregate, TAggregateId>>();

                return new EventsService<TAggregate, TAggregateId>(eventsRepo, eventsProducer);
            });
    }
}
