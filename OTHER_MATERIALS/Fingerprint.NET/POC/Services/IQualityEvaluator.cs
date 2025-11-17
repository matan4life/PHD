using MathNet.Numerics;
using MathNet.Spatial.Euclidean;
using POC.Models;

namespace POC.Services;

public interface IQualityEvaluator
{
    Task<double> EvaluateClusteringQualityAsync(IEnumerable<Cluster> clusteringResult);
}

public sealed class ScoreFunctionClusteringQualityEvaluator : IQualityEvaluator
{
    public async Task<double> EvaluateClusteringQualityAsync(IEnumerable<Cluster> clusteringResult)
    {
        var clusters = clusteringResult.ToList();
        var betweenClassDistance = await CalculateBetweenClassDistanceAsync(clusters);
        var withinClassDistance = await CalculateWithinClassDistanceAsync(clusters);
        return 1 - 1 / Math.Pow(Constants.E, Math.Pow(Constants.E, betweenClassDistance - withinClassDistance));
    }

    private static async Task<double> CalculateBetweenClassDistanceAsync(IEnumerable<Cluster> clusteringResult)
    {
        var clusters = clusteringResult.ToList();
        var pointsCount = clusters.SelectMany(x => x.Minutiae).Count();
        var centroidVectors = clusters.Select(cluster => cluster.Centroid.Point).ToList();
        var globalCentroidX = centroidVectors.Select(x => x.X).Average();
        var globalCentroidY = centroidVectors.Select(x => x.Y).Average();
        var globalCentroid = new Point2D(globalCentroidX, globalCentroidY);
        var tasks = clusters.Select(cluster => Task.Run(() => cluster.Centroid.Point.DistanceTo(globalCentroid) * (cluster.Minutiae.Count() + 1)));
        var distances = await Task.WhenAll(tasks);
        return distances.Sum() / pointsCount / clusters.Count;
    }
    
    private static async Task<double> CalculateWithinClassDistanceAsync(IEnumerable<Cluster> clusteringResult)
    {
        var clusters = clusteringResult.ToList();
        var outerTasks = clusters.Select(cluster => Task.Run(async () =>
        {
            var innerTasks = cluster.Minutiae.Select(minutia => Task.Run(() => minutia.DistanceTo(cluster.Centroid)));
            var distances = await Task.WhenAll(innerTasks);
            return distances.Sum() / (cluster.Minutiae.Count() + 1);
        }));
        var distances = await Task.WhenAll(outerTasks);
        return distances.Sum();
    }
}