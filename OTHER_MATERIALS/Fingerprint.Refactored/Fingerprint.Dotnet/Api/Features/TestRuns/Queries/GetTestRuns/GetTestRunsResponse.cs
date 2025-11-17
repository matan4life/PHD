using Api.Entities;

namespace Api.Features.TestRuns.Queries.GetTestRuns;

public sealed record GetTestRunsResponse(IEnumerable<TestRun> TestRuns);