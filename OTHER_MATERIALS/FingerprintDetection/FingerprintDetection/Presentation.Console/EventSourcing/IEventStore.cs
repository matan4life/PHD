using StereoDB.CSharp;

namespace Presentation.Console.EventSourcing;

public interface IEventStore
{
    Task<IEnumerable<IEvent<TEventId, TAggregateId>>> GetEventsAsync<TEventId, TAggregateId>(TAggregateId aggregateId)
        where TEventId : notnull
        where TAggregateId : notnull;

    Task SaveEventAsync<TEventId, TAggregateId>(IEvent<TEventId, TAggregateId> @event)
        where TEventId : notnull
        where TAggregateId : notnull;
}

public sealed class EventStore(IDatabase database) : IEventStore
{
    public async Task<IEnumerable<IEvent<TEventId, TAggregateId>>> GetEventsAsync<TEventId, TAggregateId>(TAggregateId aggregateId)
        where TEventId : notnull
        where TAggregateId : notnull
    {
        return await Task.Run(() => database.DatabaseAccessor.ReadTransaction(context =>
        {
            var events = context.UseTable(context.Schema.EventsTable);
            return events.GetIds()
                .Where(id =>
                {
                    events.TryGet(id, out var @event);
                    return @event.AggregateId.Equals(aggregateId);
                })
                .Select(id =>
                {
                    events.TryGet(id, out var @event);
                    return (IEvent<TEventId, TAggregateId>)@event;
                });
        }));
    }

    public async Task SaveEventAsync<TEventId, TAggregateId>(IEvent<TEventId, TAggregateId> @event)
        where TEventId : notnull where TAggregateId : notnull
    {
        await Task.Run(() => database.DatabaseAccessor.WriteTransaction(context =>
        {
            var events = context.UseTable(context.Schema.EventsTable);
            events.Set((IEvent<Guid, Guid>)@event);
        }));
    }
}