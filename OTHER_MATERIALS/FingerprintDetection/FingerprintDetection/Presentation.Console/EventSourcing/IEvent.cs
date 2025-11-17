using StereoDB;

namespace Presentation.Console.EventSourcing;

public interface IEvent<TEventId, TAggregateId> : IEntity<TEventId>
    where TEventId : notnull
    where TAggregateId : notnull
{
    DateTime Occurred { get; }
    
    TAggregateId AggregateId { get; }
    
    IAggregate<TAggregateId, TEventId> Apply(IAggregate<TAggregateId, TEventId> aggregate);
}