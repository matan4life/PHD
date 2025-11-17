using MediatR;

namespace Api.Features.FileSystem.Queries.GetAvailableDatasets;

public class GetAvailableDatasetsQueryHandler : IRequestHandler<GetAvailableDatasetsQuery, GetAvailableDatasetsResponse>
{
    private const string DatasetsRootFolder = @"E:\PHD\Fingerprint.Refactored\fs\datasets";
    
    public async Task<GetAvailableDatasetsResponse> Handle(GetAvailableDatasetsQuery request, CancellationToken cancellationToken)
    {
        var datasetPaths = Directory
            .GetDirectories(DatasetsRootFolder)
            .Select(Path.GetFileName);
        return new GetAvailableDatasetsResponse(datasetPaths!);
    }
}