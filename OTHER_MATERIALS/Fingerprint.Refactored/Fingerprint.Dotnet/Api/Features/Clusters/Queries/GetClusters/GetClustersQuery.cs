using MediatR;

namespace Api.Features.Clusters.Queries.GetClusters;

public sealed record GetClustersQuery(int ImageId) : IRequest<GetClustersResponse>;
