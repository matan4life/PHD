using Api.Entities;

namespace Api.Features.Clusters.Queries.GetClusters;

public sealed record GetClustersResponse(IEnumerable<Cluster> Clusters);