using Api.Models;
using MediatR;

namespace Api.Features.Comparisons.Commands.CalculateBestRotation;

public sealed record CalculateBestRotationComparisonCommand(int TestRunId) : IRequest<TelemetryResponse>;