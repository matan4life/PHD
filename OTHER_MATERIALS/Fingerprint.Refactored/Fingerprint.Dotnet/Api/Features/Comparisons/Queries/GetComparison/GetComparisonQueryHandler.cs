using Api.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Comparisons.Queries.GetComparison;

public sealed class GetComparisonQueryHandler(FingerprintContext context) : IRequestHandler<GetComparisonQuery, GetComparisonResponse>
{
    public async Task<GetComparisonResponse> Handle(GetComparisonQuery request, CancellationToken cancellationToken)
    {
        var firstLeadingMinutia = await context.ClusterMinutiae
            .Where(x => x.ClusterId == request.FirstClusterId && x.MinutiaId == request.FirstMinutiaId)
            .FirstAsync(cancellationToken: cancellationToken);

        var secondLeadingMinutia = await context.ClusterMinutiae
            .Where(x => x.ClusterId == request.SecondClusterId && x.MinutiaId == request.SecondMinutiaId)
            .FirstAsync(cancellationToken: cancellationToken);

        var firstCentroid = await context.ClusterMinutiae
            .Where(x => x.ClusterId == request.FirstClusterId && x.IsCentroid)
            .FirstAsync(cancellationToken: cancellationToken);

        var secondCentroid = await context.ClusterMinutiae
            .Where(x => x.ClusterId == request.SecondClusterId && x.IsCentroid)
            .FirstAsync(cancellationToken: cancellationToken);

        var comparisons = await context.ClusterComparisons
            .Where(x => x.LeadingFirstMinutiaId == firstLeadingMinutia.Id && x.LeadingSecondMinutiaId == secondLeadingMinutia.Id && x.Matches)
            .Include(x => x.FirstMinutia)
            .Include(x => x.SecondMinutia)
            .ToListAsync(cancellationToken: cancellationToken);
        
        var details = comparisons.Select(x => new ComparisonDotDetails(x.FirstMinutia.MinutiaId, x.SecondMinutia.MinutiaId));

        return new GetComparisonResponse(details);
    }
}
