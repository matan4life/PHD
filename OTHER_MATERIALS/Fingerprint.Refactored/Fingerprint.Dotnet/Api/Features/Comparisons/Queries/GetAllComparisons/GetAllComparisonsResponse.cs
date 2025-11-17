namespace Api.Features.Comparisons.Queries.GetAllComparisons;

public sealed record GetAllComparisonsResponse(IEnumerable<Response> Results);

public class Response
{
    public string FirstImage { get; set; }
    public string SecondImage { get; set; }
    public int Count { get; set; }
}