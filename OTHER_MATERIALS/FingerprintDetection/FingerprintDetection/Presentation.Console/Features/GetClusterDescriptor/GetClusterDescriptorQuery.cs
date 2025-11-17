using MediatR;
using Presentation.Console.Models;

namespace Presentation.Console.Features.GetClusterDescriptor;

public sealed record GetClusterDescriptorQuery(Cluster Cluster) : IRequest<GetClusterDescriptorResponse>;