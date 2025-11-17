using MediatR;

namespace Api.Features.FileSystem.Queries.GetAvailableDatasets;

public sealed record GetAvailableDatasetsQuery() : IRequest<GetAvailableDatasetsResponse>;