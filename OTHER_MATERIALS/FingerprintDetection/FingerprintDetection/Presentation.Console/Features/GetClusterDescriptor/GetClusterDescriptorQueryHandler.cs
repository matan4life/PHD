using MediatR;
using Presentation.Console.Models;

namespace Presentation.Console.Features.GetClusterDescriptor;

public sealed class GetClusterDescriptorQueryHandler : IRequestHandler<GetClusterDescriptorQuery, GetClusterDescriptorResponse>
{
    public Task<GetClusterDescriptorResponse> Handle(GetClusterDescriptorQuery request, CancellationToken cancellationToken)
    {
        var clusterDescriptor = new ClusterDescriptor(request.Cluster);
        return Task.FromResult(new GetClusterDescriptorResponse(clusterDescriptor));
    }
}