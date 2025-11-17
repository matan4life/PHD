using Api.Models;
using MediatR;

namespace Api.Features.FileSystem.Commands.UploadFiles;

public sealed record UploadFileCommand(string DatasetPath, IFormFileCollection FileCollection) : IRequest<TelemetryResponse>;