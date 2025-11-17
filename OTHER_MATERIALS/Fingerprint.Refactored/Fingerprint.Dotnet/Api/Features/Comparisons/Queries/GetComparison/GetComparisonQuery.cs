using MediatR;

namespace Api.Features.Comparisons.Queries.GetComparison;

public sealed record GetComparisonQuery(int FirstClusterId,
    int SecondClusterId,
    int FirstMinutiaId,
    int SecondMinutiaId) : IRequest<GetComparisonResponse>;
