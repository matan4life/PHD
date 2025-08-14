namespace Api.Features.FileSystem.Queries.GetAvailableDatasets;

public sealed record GetAvailableDatasetsResponse(IEnumerable<string> DatasetPaths);