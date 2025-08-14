using Api.Models;

namespace Api.Features.TestRuns.Commands.CreateTestRun;

public sealed record CreateTestRunResponse(TelemetryResponse Telemetry, int TestRunId);