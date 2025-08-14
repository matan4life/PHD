using Api.Database;
using Api.Entities;
using Api.Extensions;
using Api.Models;
using EFCore.BulkExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Clusters.Commands.CreateClusters;

public sealed class CreateClusterCommandHandler(FingerprintContext context)
    : IRequestHandler<CreateClusterCommand, TelemetryResponse>
{
    public async Task<TelemetryResponse> Handle(CreateClusterCommand request, CancellationToken cancellationToken)
    {
        var dateTime = DateTime.Now;
        var images = await context.Images
            .Where(image => image.TestRunId == request.TestRunId && image.ProcessedCorrectly == true)
            .Include(image => image.Minutiae)
            .ToListAsync(cancellationToken);
        var clusters = (await images.ParallelSelectAsync(CreateClustersAsync)).SelectMany(x => x).ToList();
        await context.AddRangeAsync(clusters.Select(x => x.Item2), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        var minutiae =
            await clusters.ParallelSelectAsync(tuple =>
                CreateClusterMinutiaeAsync(tuple.Item1, tuple.Item3, tuple.Item2));
        await context.BulkInsertAsync(minutiae.SelectMany(x => x), cancellationToken: cancellationToken);
        var endTime = DateTime.Now;
        return new TelemetryResponse(dateTime, endTime, endTime - dateTime);
    }

    private static async Task<IEnumerable<(Image, Cluster, Minutia)>> CreateClustersAsync(Image image)
    {
        var globalCentroid = new
        {
            X = image.Minutiae.Average(minutia => minutia.X),
            Y = image.Minutiae.Average(minutia => minutia.Y)
        };
        var centroids = image.Minutiae
            .OrderBy(minutia => minutia.DistanceTo(globalCentroid.X, globalCentroid.Y))
            .Take(3)
            .ToList();
        var clusters = centroids.Select(centroid => (image, new Cluster
            {
                ImageId = image.Id
            }, centroid))
            .ToList();
        return clusters;
    }

    private static async Task<IEnumerable<ClusterMinutiae>> CreateClusterMinutiaeAsync(Image image, Minutia centroid,
        Cluster cluster)
    {
        var minutiaeInCluster = image.Minutiae
            .Where(minutia => minutia.Id != centroid.Id)
            .OrderBy(minutia => minutia.DistanceTo(centroid))
            .Take(15)
            .Select(minutia => new ClusterMinutiae
            {
                ClusterId = cluster.Id,
                MinutiaId = minutia.Id,
                IsCentroid = false
            });
        return
        [
            new ClusterMinutiae
            {
                MinutiaId = centroid.Id,
                ClusterId = cluster.Id,
                IsCentroid = true
            },
            ..minutiaeInCluster
        ];
    }
}