using Api.Models;
using MediatR;

namespace Api.Features.Metrics.Commands.CalculateMetrics;

public sealed record CalculateMetricsCommand(int TestRunId) : IRequest<TelemetryResponse>;