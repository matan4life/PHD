using Api.Models;
using MediatR;

namespace Api.Features.Images.Commands.ProcessImages;

public sealed record ProcessImagesCommand(int TestRunId) : IRequest<TelemetryResponse>;