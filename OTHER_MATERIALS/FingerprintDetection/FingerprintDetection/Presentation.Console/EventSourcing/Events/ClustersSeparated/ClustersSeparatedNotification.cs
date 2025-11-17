using MediatR;
using Presentation.Console.Models;
using ClusterInformation = (Presentation.Console.Models.Cluster, Presentation.Console.Models.ClusterDescriptor);

namespace Presentation.Console.EventSourcing.Events.ClustersSeparated;

public sealed record ClustersSeparatedNotification(
    Guid AggregateId,
    IEnumerable<ClusterInformation> Clusters,
    string FileName) : INotification;