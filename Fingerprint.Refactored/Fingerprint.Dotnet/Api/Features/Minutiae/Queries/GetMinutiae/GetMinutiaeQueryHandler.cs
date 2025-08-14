using Api.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Minutiae.Queries.GetMinutiae;

public sealed class GetMinutiaeQueryHandler(FingerprintContext context) : IRequestHandler<GetMinutiaeQuery, GetMinutiaeResponse>
{
    public async Task<GetMinutiaeResponse> Handle(GetMinutiaeQuery request, CancellationToken cancellationToken)
    {
        var minutiae = await context.Minutiae
            .Where(m => m.ImageId == request.ImageId)
            .ToListAsync(cancellationToken: cancellationToken);
        return new GetMinutiaeResponse(minutiae);
    }
}
