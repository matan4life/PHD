using Api.Models;
using MediatR;

namespace Api.Features.FileSystem.Commands.UploadFiles;

public sealed class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, TelemetryResponse>
{
    private const string DatasetsRootFolder = @"E:\PHD\Fingerprint.Refactored\fs\datasets";
    
    public async Task<TelemetryResponse> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        var dateTime = DateTime.Now;
        var datasetPath = Path.Combine(DatasetsRootFolder, request.DatasetPath);
        Directory.CreateDirectory(datasetPath);
        
        foreach (var file in request.FileCollection)
        {
            var filePath = Path.Combine(datasetPath, file.FileName);
            await using var fileStream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(fileStream, cancellationToken);
        }
        var endTime = DateTime.Now;
        return new TelemetryResponse(dateTime, endTime, endTime - dateTime);
    }
}