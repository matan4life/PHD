using Api.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Comparisons.Queries.GetComparisonAggregate;

public sealed class GetComparisonAggregateQueryHandler(FingerprintContext context) : IRequestHandler<GetComparisonAggregateQuery, GetComparisonAggregateResponse>
{
    public async Task<GetComparisonAggregateResponse> Handle(GetComparisonAggregateQuery request, CancellationToken cancellationToken)
    {
        var distanceMetric = await context.Metrics
            .Where(x => x.Name == "Distance")
            .FirstAsync(cancellationToken: cancellationToken);
        var angleMetric = await context.Metrics
            .Where(x => x.Name == "Angle")
            .FirstAsync(cancellationToken: cancellationToken);

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
            .Where(x => x.LeadingFirstMinutiaId == firstLeadingMinutia.Id && x.LeadingSecondMinutiaId == secondLeadingMinutia.Id)
            .Include(x => x.FirstMinutia)
            .Include(x => x.SecondMinutia)
            .ToListAsync(cancellationToken: cancellationToken);

        return new GetComparisonAggregateResponse(comparisons.Select(x => new Comparison(x.FirstMinutia.MinutiaId, x.SecondMinutia.MinutiaId, x.Matches, new ComparisonDetails(x.DistanceDifference, distanceMetric.AcceptableThreshold, x.DistanceDifference <= distanceMetric.AcceptableThreshold), x.AngleDifference.HasValue ? new ComparisonDetails(x.AngleDifference.Value, angleMetric.AcceptableThreshold, x.AngleDifference.Value <= angleMetric.AcceptableThreshold) : null)));
    }
}
