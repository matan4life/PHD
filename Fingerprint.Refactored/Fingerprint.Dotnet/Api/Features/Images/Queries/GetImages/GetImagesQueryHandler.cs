using Api.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Images.Queries.GetImages;

public sealed class GetImagesQueryHandler(FingerprintContext context) : IRequestHandler<GetImagesQuery, GetImagesResponse>
{
    public async Task<GetImagesResponse> Handle(GetImagesQuery request, CancellationToken cancellationToken)
    {
        var existingImage = await context.Images.FindAsync([request.ImageId], cancellationToken);
        var idToFilter = existingImage?.Id ?? 0;

        var images = await context.Images
            .Where(i => i.TestRunId == request.TestRunId && i.ProcessedCorrectly == true && i.Id > idToFilter)
            .ToListAsync(cancellationToken: cancellationToken);
        return new GetImagesResponse(images);
    }
}