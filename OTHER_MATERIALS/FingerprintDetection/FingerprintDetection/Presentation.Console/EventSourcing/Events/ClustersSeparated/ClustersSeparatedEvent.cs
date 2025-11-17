using VectSharp;

namespace Presentation.Console.EventSourcing.Events.ClustersSeparated;

public sealed record ClustersSeparatedEvent(Guid AggregateId, Page ClustersPage)
    : EventBase<Guid, VisualizationAggregate, Guid>
{
    private const string PageName = "clusters_distribution";
    
    public override Guid Id { get; } = Guid.NewGuid();

    public override Guid AggregateId { get; } = AggregateId;
    
    protected override VisualizationAggregate Apply(VisualizationAggregate aggregate)
    {
        aggregate.Document.Pages.Add(ClustersPage);
        aggregate.PageNames.Add(PageName);
        return aggregate;
    }
}