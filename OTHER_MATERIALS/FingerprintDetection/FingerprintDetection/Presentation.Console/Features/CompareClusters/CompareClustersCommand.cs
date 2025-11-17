using MediatR;
using Presentation.Console.Models;

namespace Presentation.Console.Features.CompareClusters;

public sealed record CompareClustersCommand(IEnumerable<Cluster> FirstClusters, IEnumerable<Cluster> SecondClusters)
    : IRequest<CompareClustersResponse>;