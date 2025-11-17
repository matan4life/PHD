using MediatR;
using Presentation.Console.Models;

namespace Presentation.Console.Features.SeparateByClusters;

public sealed class SeparateByClustersCommandHandler
    : IRequestHandler<SeparateByClustersCommand, SeparateByClustersResponse>
{
    public Task<SeparateByClustersResponse> Handle(SeparateByClustersCommand request,
        CancellationToken cancellationToken)
    {
        var minutiae = request.Minutiae.ToList();
        var globalCentroidX = Convert.ToInt32(minutiae.Average(minutia => minutia.X));
        var globalCentroidY = Convert.ToInt32(minutiae.Average(minutia => minutia.Y));
        var orderedMinutiae =
            minutiae.OrderBy(minutia => minutia.DistanceTo(new Minutia(globalCentroidX, globalCentroidY, 0)));
        var clusterCenters = orderedMinutiae.Take(3).ToList();
        var clusters = clusterCenters.Select(center =>
        {
            var clusterMinutiae = minutiae.Except([center])
                .OrderBy(minutia => minutia.DistanceTo(center))
                .Take(15)
                .ToList();

            return new Cluster(center, (int)clusterMinutiae.Last().DistanceTo(center), clusterMinutiae.ToList());
        });

        return Task.FromResult(new SeparateByClustersResponse(clusters.Where(cluster => cluster.Minutiae.Count() > 1)
            .OrderByDescending(x => x.Minutiae.Count()))); 
    }
}