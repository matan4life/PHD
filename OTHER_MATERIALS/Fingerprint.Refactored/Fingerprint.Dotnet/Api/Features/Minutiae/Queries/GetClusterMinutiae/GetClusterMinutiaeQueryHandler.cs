using Api.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Minutiae.Queries.GetClusterMinutiae;

public sealed class GetClusterMinutiaeQueryHandler(FingerprintContext context) : IRequestHandler<GetClusterMinutiaeQuery, GetClusterMinutiaeResponse>
{
    public async Task<GetClusterMinutiaeResponse> Handle(GetClusterMinutiaeQuery request, CancellationToken cancellationToken)
    {
        var minutiae = await context.ClusterMinutiae
            .Include(m => m.Minutia)
            .Where(m => m.ClusterId == request.ClusterId)
            .ToListAsync(cancellationToken: cancellationToken);
        var centroid = minutiae.First(x => x.IsCentroid);

        return new GetClusterMinutiaeResponse(centroid.Minutia, minutiae.Where(x => x.MinutiaId != centroid.MinutiaId).Select(x => x.Minutia));
    }
}
