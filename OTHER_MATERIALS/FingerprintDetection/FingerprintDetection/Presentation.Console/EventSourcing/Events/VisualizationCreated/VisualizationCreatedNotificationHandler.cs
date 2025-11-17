using MediatR;

namespace Presentation.Console.EventSourcing.Events.VisualizationCreated;

public sealed class VisualizationCreatedNotificationHandler(IEventStore eventStore)
    : INotificationHandler<VisualizationCreatedNotification>
{
    public async Task Handle(VisualizationCreatedNotification notification, CancellationToken cancellationToken)
    {
        var visualizationCreatedEvent = new VisualizationCreatedEvent(notification.Name, notification.AggregateId);
        await eventStore.SaveEventAsync(visualizationCreatedEvent);
    }
}