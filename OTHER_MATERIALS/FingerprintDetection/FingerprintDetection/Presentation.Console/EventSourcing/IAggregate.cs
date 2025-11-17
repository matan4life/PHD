namespace Presentation.Console.EventSourcing;

public interface IAggregate<TAggregateId, TEventId> where TAggregateId : notnull where TEventId : notnull
{
    TAggregateId AggregateId { get; }

    IAggregate<TAggregateId, TEventId> Apply(
        IEnumerable<IEvent<TEventId, TAggregateId>> events);
}