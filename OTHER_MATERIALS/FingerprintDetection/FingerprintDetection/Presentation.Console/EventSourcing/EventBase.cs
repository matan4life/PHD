namespace Presentation.Console.EventSourcing;

public abstract record EventBase<TEventId, TAggregate, TAggregateId>
    : IEvent<TEventId, TAggregateId>
    where TEventId : notnull
    where TAggregateId : notnull
    where TAggregate : IAggregate<TAggregateId, TEventId>
{
    public abstract TEventId Id { get; }
    
    public abstract TAggregateId AggregateId { get; }
    
    public DateTime Occurred { get; } = DateTime.Now;
    
    public IAggregate<TAggregateId, TEventId> Apply(IAggregate<TAggregateId, TEventId> aggregate)
    {
        var castedAggregate = (TAggregate)aggregate;
        return Apply(castedAggregate);
    }
    
    protected abstract TAggregate Apply(TAggregate aggregate);
}