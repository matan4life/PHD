using Api.Models;
using MediatR;

namespace Api.Features.Clusters.Commands.CreateClusters;

public sealed record CreateClusterCommand(int TestRunId) : IRequest<TelemetryResponse>;