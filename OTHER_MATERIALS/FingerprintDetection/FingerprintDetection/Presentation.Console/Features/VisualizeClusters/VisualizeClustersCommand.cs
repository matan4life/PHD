using MediatR;
using Presentation.Console.Models;

namespace Presentation.Console.Features.VisualizeClusters;

public sealed record VisualizeClustersCommand(string ImagePath, IEnumerable<Cluster> Clusters) : IRequest;