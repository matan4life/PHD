using Api.Database;
using Api.Options;
using Api.Services;
using MediatR;
using Microsoft.Extensions.Options;
using System.Drawing;
using System.Drawing.Imaging;

namespace Api.Features.FileSystem.Queries.GetBackgroundImage;

public sealed class GetBackgroundImageQueryHandler(
    FingerprintContext context,
    IFileService fileService,
    IOptions<EnvironmentVariableOptions>? options)
    : IRequestHandler<GetBackgroundImageQuery, GetBackgroundImageResponse>
{
    private EnvironmentVariableOptions Options { get; } =
        options?.Value ?? throw new ArgumentNullException(nameof(options));

    public async Task<GetBackgroundImageResponse> Handle(GetBackgroundImageQuery request,
        CancellationToken cancellationToken)
    {
        var rootFolder = request.BackgroundType switch
        {
            BackgroundType.Enhanced => Options.FlaskEnhancedOutputFolder,
            BackgroundType.Skeleton => Options.FlaskSkeletonOutputFolder,
            _ => Options.FlaskInputFolder
        };
        var image = await context.Images.FindAsync([request.ImageId], cancellationToken: cancellationToken);
        var filePath = $@"{rootFolder}\{image.TestRunId}\{image.FileName}";
        var tiffStream = await fileService.GetAsync(filePath);
        var pngStream = new MemoryStream();
        Bitmap.FromStream(tiffStream).Save(pngStream, ImageFormat.Png);
        pngStream.Seek(0, SeekOrigin.Begin);
        return new GetBackgroundImageResponse(pngStream);
    }
}