using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Units;
using Presentation.Console.Extensions;

namespace Presentation.Console.Models;

public sealed class ClusterDescriptor
{
    private const int DistanceThreshold = 10;

    public readonly Cluster Cluster;

    public ClusterDescriptor(Cluster cluster)
    {
        Cluster = cluster;
        DistanceDescriptor = CreateDistanceDescriptor();
        AnglesDescriptor = CreateAnglesDescriptor();
    }

    public Matrix<double> DistanceDescriptor { get; }

    public Matrix<double> AnglesDescriptor { get; }

    public int GetMaximumEquivalenceScore(ClusterDescriptor other)
    {
        var indices = new ShiftedIndexSequence(DistanceDescriptor.ColumnCount);
        var otherIndices = new ShiftedIndexSequence(other.DistanceDescriptor.ColumnCount);
        var maxScore = 0;
        for (var firstRotation = 0; firstRotation < indices.Count(); firstRotation++)
        {
            indices.ShiftRight();
            var firstDistanceIndex = indices.ElementAt(1);
            var initVectorLength = DistanceDescriptor[2, firstDistanceIndex];
            for (var secondRotation = 0; secondRotation < otherIndices.Count(); secondRotation++)
            {
                otherIndices.ShiftRight();
                var secondDistanceIndex = otherIndices.ElementAt(1);
                var otherInitVectorLength = other.DistanceDescriptor[2, secondDistanceIndex];
                if (Math.Abs(initVectorLength - otherInitVectorLength) > DistanceThreshold)
                {
                    continue;
                }

                var score = indices.Count(index => otherIndices.Any(otherIndex =>
                {
                    IEnumerable<double> firstAngles =
                    [
                        ..AnglesDescriptor.Row(index).ToArray().Take(index),
                        ..AnglesDescriptor.Row(index).ToArray().Skip(index + 1)
                    ];
                    IEnumerable<double> secondAngles =
                    [
                        ..other.AnglesDescriptor.Row(otherIndex).ToArray().Take(otherIndex),
                        ..other.AnglesDescriptor.Row(otherIndex).ToArray().Skip(otherIndex + 1)
                    ];
                    return new ClusterComparisonPair(
                        DistanceDescriptor[2, index],
                        other.DistanceDescriptor[2, otherIndex],
                        firstAngles.Select(Angle.FromDegrees),
                        secondAngles.Select(Angle.FromDegrees)).IsEquivalent();
                }));
                
                if (score > maxScore)
                {
                    maxScore = score;
                }
            }
        }

        return maxScore;
    }

    private Matrix<double> CreateDistanceDescriptor()
    {
        var matrix = new double[3, Cluster.Minutiae.Count()];

        foreach (var (minutia, index) in GetOrderedMinutiaeWithIndex())
        {
            matrix[0, index] = minutia.X;
            matrix[1, index] = minutia.Y;
            matrix[2, index] = minutia.DistanceTo(Cluster.Centroid);
        }

        return Matrix<double>.Build.DenseOfArray(matrix);
    }

    private Matrix<double> CreateAnglesDescriptor()
    {
        var orderedMinutiae = GetOrderedMinutiaeWithIndex().ToList();
        var matrix = new double[orderedMinutiae.Count, orderedMinutiae.Count];
        foreach (var (minutia, index) in orderedMinutiae)
        {
            foreach (var (otherMinutia, otherIndex) in orderedMinutiae)
            {
                if (index == otherIndex)
                {
                    matrix[index, otherIndex] = 0;
                }
                else
                {
                    var angleDifference = otherMinutia.PolarAngleFromCenter(Cluster.Centroid).NormalizeAngle()
                                          - minutia.PolarAngleFromCenter(Cluster.Centroid).NormalizeAngle();
                    matrix[index, otherIndex] = angleDifference.NormalizeAngle().Degrees;
                }
            }
        }

        return Matrix<double>.Build.DenseOfArray(matrix);
    }

    private IEnumerable<(Minutia minutia, int index)> GetOrderedMinutiaeWithIndex()
    {
        return Cluster.Minutiae
            .OrderBy(minutia => minutia.PolarAngleFromCenter(Cluster.Centroid).NormalizeAngle().Degrees)
            .Select((value, i) => (value, i));
    }
}