using Api.Models;
using MediatR;

namespace Api.Features.TestRuns.Commands.CreateTestRun;

public sealed record CreateTestRunCommand(string DatasetName) : IRequest<CreateTestRunResponse>;