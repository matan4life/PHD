namespace Api.Features.Comparisons.Queries.GetComparisonAggregate;

public sealed record GetComparisonAggregateResponse(IEnumerable<Comparison> Comparisons);

public sealed record Comparison(int FirstMinutiaId, int SecondMinutiaId, bool IsMatch, ComparisonDetails DistanceDetails, ComparisonDetails? AngleDetails);

public sealed record ComparisonDetails(double Value, double AcceptableThreshold, bool Status);