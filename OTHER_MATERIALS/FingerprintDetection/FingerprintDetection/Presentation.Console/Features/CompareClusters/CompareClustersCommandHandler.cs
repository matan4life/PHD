using MathNet.Numerics.LinearAlgebra;
using MediatR;
using Presentation.Console.Features.GetClusterDescriptor;

namespace Presentation.Console.Features.CompareClusters;

public sealed class CompareClustersCommandHandler(ISender sender)
    : IRequestHandler<CompareClustersCommand, CompareClustersResponse>
{
    public async Task<CompareClustersResponse> Handle(CompareClustersCommand request,
        CancellationToken cancellationToken)
    {
        var matrix = new double[request.FirstClusters.Count(), request.SecondClusters.Count()];
        foreach (var (firstCluster, i) in request.FirstClusters.OrderByDescending(x => x.Minutiae.Count())
                     .Select((x, i) => (x, i)))
        {
            foreach (var (secondCluster, j) in request.SecondClusters.OrderByDescending(x => x.Minutiae.Count())
                         .Select((x, j) => (x, j)))
            {
                var firstDescriptor = await sender.Send(new GetClusterDescriptorQuery(firstCluster), cancellationToken);
                var secondDescriptor =
                    await sender.Send(new GetClusterDescriptorQuery(secondCluster), cancellationToken);
                matrix[i, j] =
                    firstDescriptor.ClusterDescriptor.GetMaximumEquivalenceScore(secondDescriptor.ClusterDescriptor);
            }
        }
        
        return new CompareClustersResponse(Matrix<double>.Build.DenseOfArray(matrix));
    }
}