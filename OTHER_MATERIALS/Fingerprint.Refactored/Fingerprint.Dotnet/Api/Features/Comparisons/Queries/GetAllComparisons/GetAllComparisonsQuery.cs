using MediatR;

namespace Api.Features.Comparisons.Queries.GetAllComparisons;

public sealed record GetAllComparisonsQuery(int FirstClusterId, int SecondClusterId, int ThirdClusterId) : IRequest<int>;