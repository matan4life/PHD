using MediatR;

namespace Api.Features.TestRuns.Queries.GetTestRuns;

public sealed record GetTestRunsQuery : IRequest<GetTestRunsResponse>;