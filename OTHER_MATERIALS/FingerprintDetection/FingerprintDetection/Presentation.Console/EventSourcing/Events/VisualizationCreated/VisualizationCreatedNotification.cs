using MediatR;

namespace Presentation.Console.EventSourcing.Events.VisualizationCreated;

public sealed record VisualizationCreatedNotification(Guid AggregateId, string Name) : INotification;