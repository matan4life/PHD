using Api.Database;
using Api.Entities;
using Api.Extensions;
using Api.Models;
using Api.Options;
using Api.Services;
using MediatR;
using Microsoft.Extensions.Options;

namespace Api.Features.Images.Commands.ProcessImages;

public sealed class ProcessImagesCommandHandler(
    FingerprintContext context,
    IHttpClientFactory factory,
    IFileService fileService,
    IOptions<EnvironmentVariableOptions>? options) : IRequestHandler<ProcessImagesCommand, TelemetryResponse>
{
    private HttpClient Client { get; } = factory.CreateClient();

    private EnvironmentVariableOptions Options { get; } =
        options?.Value ?? throw new ArgumentNullException(nameof(options));

    public async Task<TelemetryResponse> Handle(ProcessImagesCommand request, CancellationToken cancellationToken)
    {
        var dateTime = DateTime.Now;
        var testRun = await context.TestRuns.FindAsync([request.TestRunId], cancellationToken: cancellationToken) ??
                      throw new NullReferenceException();
        var filePaths = Directory.GetFiles(testRun.DatasetPath);
        await filePaths.ParallelForEachAsync(path => fileService.CopyAsync(path, $@"{Options.FlaskInputFolder}\{testRun.Id}"));
        var images = filePaths
            .Select(path => new Image
            {
                FileName = Path.GetFileName(path),
                TestRunId = testRun.Id
            })
            .OrderBy(x => x.FileName)
            .ToList();
        await context.AddRangeAsync(images, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await images.ParallelForEachAsync(image =>
            Client.PostAsync(
                $"http://localhost:3000/image?image_id={image.Id}&test_run_id={request.TestRunId}", default,
                cancellationToken));
        var endTime = DateTime.Now;
        return new TelemetryResponse(dateTime, endTime, endTime - dateTime);
    }
}