namespace Presentation.Console.EventSourcing.Events.VisualizationCreated;

public sealed record VisualizationCreatedEvent(string Name, Guid AggregateId) : EventBase<Guid, VisualizationAggregate, Guid>
{
    public override Guid Id { get; } = Guid.NewGuid();
    
    public override Guid AggregateId { get; } = AggregateId;

    protected override VisualizationAggregate Apply(VisualizationAggregate aggregate)
    {
        aggregate.Name = Name;
        return aggregate;
    }
}