namespace Api.Features.Comparisons.Queries.GetComparison;

public sealed record GetComparisonResponse(IEnumerable<ComparisonDotDetails> DotDetails);

public sealed record ComparisonDotDetails(int FirstMinutiaId, int SecondMinutiaId);