using Api.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Clusters.Queries.GetClusters;

public sealed class GetClustersQueryHandler(FingerprintContext context) : IRequestHandler<GetClustersQuery, GetClustersResponse>
{
    public async Task<GetClustersResponse> Handle(GetClustersQuery request, CancellationToken cancellationToken)
    {
        var clusters = await context.Clusters
            .Where(c => c.ImageId == request.ImageId)
            .ToListAsync(cancellationToken: cancellationToken);
        return new GetClustersResponse(clusters);
    }
}
