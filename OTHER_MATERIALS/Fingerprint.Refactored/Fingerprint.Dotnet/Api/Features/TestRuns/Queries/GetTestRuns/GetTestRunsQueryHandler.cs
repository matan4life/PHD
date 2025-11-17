using Api.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.TestRuns.Queries.GetTestRuns;

public sealed class GetTestRunsQueryHandler(FingerprintContext context)
    : IRequestHandler<GetTestRunsQuery, GetTestRunsResponse>
{
    public async Task<GetTestRunsResponse> Handle(GetTestRunsQuery request, CancellationToken cancellationToken)
    {
        return new GetTestRunsResponse(await context.TestRuns.ToListAsync(cancellationToken: cancellationToken));
    }
}