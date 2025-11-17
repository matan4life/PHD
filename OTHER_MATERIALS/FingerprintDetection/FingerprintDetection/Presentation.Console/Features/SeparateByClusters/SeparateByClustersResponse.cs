using Presentation.Console.Models;

namespace Presentation.Console.Features.SeparateByClusters;

public sealed record SeparateByClustersResponse(IEnumerable<Cluster> Clusters);
