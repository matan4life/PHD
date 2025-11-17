using VectSharp;

namespace Presentation.Console.EventSourcing;

public sealed class VisualizationAggregate : IAggregate<Guid, Guid>
{
    private VisualizationAggregate(Guid aggregateId)
    {
        AggregateId = aggregateId;
        PageNames = new List<string>();
        Document = new Document();
    }
    
    public Guid AggregateId { get; }

    public string? Name { get; set; }

    public Document Document { get; set; }

    public IList<string> PageNames { get; set; }

    public IAggregate<Guid, Guid> Apply(IEnumerable<IEvent<Guid, Guid>> events)
    {
        IAggregate<Guid, Guid> aggregate = this;
        return events.Aggregate(aggregate, (current, @event) => @event.Apply(current));
    }
    
    public static VisualizationAggregate Create(Guid aggregateId)
    {
        return new VisualizationAggregate(aggregateId);
    }
}