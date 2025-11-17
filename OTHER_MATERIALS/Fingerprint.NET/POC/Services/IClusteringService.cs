using POC.Extensions;
using POC.Models;

namespace POC.Services;

public interface IClusteringService
{
    Task<IEnumerable<Cluster>> ClusterAsync(IEnumerable<CartesianMinutia> minutiae, int radius);
}

public sealed class CMedoidsClusteringService : IClusteringService
{
    public async Task<IEnumerable<Cluster>> ClusterAsync(IEnumerable<CartesianMinutia> minutiae, int radius)
    {
        // Get mass center of minutiae
        var cartesianMinutiae = minutiae.ToList();
        var massCenterX = cartesianMinutiae.Average(x => x.X);
        var massCenterY = cartesianMinutiae.Average(x => x.Y);
        var clusterCenters = cartesianMinutiae.OrderBy(x => x.DistanceTo(new CartesianMinutia
        {
            X = (int)massCenterX,
            Y = (int)massCenterY
        }))
            .Take(3)
            .ToList();
        var startRadius = radius;
        List<CartesianMinutia> remainingMinutiae;
        var clusters = new List<Cluster>();
        do
        {
            clusters.Clear();
            remainingMinutiae = [..cartesianMinutiae];
            foreach (var center in clusterCenters)
            {
                var clusterMinutiae = cartesianMinutiae
                    .Where(x => x.DistanceTo(center) <= startRadius)
                    .Except([center])
                    .ToList();
                remainingMinutiae.RemoveAll(clusterMinutiae.Contains);
                clusters.Add(new Cluster
                {
                    Centroid = center,
                    Minutiae = clusterMinutiae,
                    Radius = startRadius
                });
            }
            startRadius += 10;
        } while (remainingMinutiae.Count > 0.5 * cartesianMinutiae.Count);

        return clusters;
    }
}

public sealed class ForelClusteringService : IClusteringService
{
    private Random Random { get; } = new();
    
    public async Task<IEnumerable<Cluster>> ClusterAsync(IEnumerable<CartesianMinutia> minutiae, int radius)
    {
        /*
         * Generate FOREL algorithm basing on next steps:
         * 1. Randomly select a point from the set of minutiae - it will be the centroid of the first cluster
         * 2. Find all minutiae that are within the radius of the centroid.
         * 3. Calculate the new centroid of the cluster.
         * 4. Repeat steps 2 and 3 until the centroid does not change.
         * 5. Remove all minutiae that are part of the cluster from the set of minutiae.
         * 6. Repeat steps 1-5 until the set of minutiae is empty.
         * 7. Return the clusters.
         */
        var clusters = new List<Cluster>();
        var minutiaeList = minutiae.ToList();
        while (minutiaeList.Count > 0)
        {
            var currentCentroid = minutiaeList.GetFirstFromTopLeftCornerMinutia();
            var newCentroid = await GetNewCentroidAsync(currentCentroid, minutiaeList, radius);
            while (newCentroid != currentCentroid)
            {
                currentCentroid = newCentroid;
                newCentroid = await GetNewCentroidAsync(currentCentroid, minutiaeList, radius);
            }
            var cluster = new Cluster
            {
                Centroid = newCentroid,
                Radius = radius,
                Minutiae = minutiaeList.Where(x =>
                {
                    var distance = x.DistanceTo(newCentroid);
                    return distance <= radius && distance > 0;
                })
                .OrderBy(x => x.GetPolarAngle(newCentroid).Normalize())
            };
            clusters.Add(cluster);
            minutiaeList = minutiaeList.Except(cluster.Minutiae.Append(newCentroid)).ToList();
        }
        return clusters.Where(x => x.Minutiae.Count() > 1);
    }

    private static async Task<CartesianMinutia> GetNewCentroidAsync(CartesianMinutia currentCentroid,
        IEnumerable<CartesianMinutia> availableMinutiae, int radius)
    {
        var radiusNeighbours = availableMinutiae
            .Where(x => x.DistanceTo(currentCentroid) <= radius)
            .ToList();
        var outerTasks = radiusNeighbours.Select(minutia => Task.Run(async () =>
        {
            var innerTasks = radiusNeighbours.Select(neighbour => Task.Run(() => neighbour.DistanceTo(minutia)));
            var results = await Task.WhenAll(innerTasks);
            return (results.Sum(), minutia);
        }));
        var result = await Task.WhenAll(outerTasks);
        return result.MinBy(x => x.Item1).Item2;
    }
}