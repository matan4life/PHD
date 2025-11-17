using MediatR;

namespace Api.Features.Comparisons.Queries.GetComparisonAggregate;

public sealed record GetComparisonAggregateQuery(int FirstClusterId,
    int SecondClusterId,
    int FirstMinutiaId,
    int SecondMinutiaId) : IRequest<GetComparisonAggregateResponse>;