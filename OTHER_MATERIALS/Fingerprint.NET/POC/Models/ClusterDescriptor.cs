using MathNet.Numerics;
using MathNet.Spatial.Units;

namespace POC.Models;

public sealed class ClusterDescriptor(double[,] distanceMatrix, double[,] angleMatrix)
{
    public double[,] DistanceMatrix { get; } = distanceMatrix;

    public double[,] AngleMatrix { get; } = angleMatrix;

    public IDictionary<int, int> DistancePointers { get; private set; } = Enumerable.Range(0, distanceMatrix.GetLength(1))
        .ToDictionary(x => x, x => x);

    public static ClusterDescriptor Create(Cluster cluster)
    {
        return new ClusterDescriptor(cluster.MapToDistanceMatrix(), cluster.MapToAnglesMatrix());
    }

    public void CyclicShiftRight()
    {
        var first = DistancePointers.First();
        DistancePointers = DistancePointers.Skip(1)
            .Select(x => new KeyValuePair<int, int>(x.Key + 1 == DistancePointers.Keys.Count
                    ? 1
                    : x.Key + 1,
                x.Value))
            .Append(first)
            .OrderBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Value);
    }

    public bool AreFirstDescriptorPointsEqual(ClusterDescriptor other)
    {
        var currentFirstElement = DistancePointers[1];
        var otherFirstElement = other.DistancePointers[1];
        return Math.Abs(DistanceMatrix[2, currentFirstElement] - other.DistanceMatrix[2, otherFirstElement]) < 10;
    }

    public bool ArePointsEqual(ClusterDescriptor other, int index)
    {
        var currentElementPoints = DistancePointers[index];
        var otherElementPoints = other.DistancePointers[index];
        var currentLength = DistanceMatrix[2, currentElementPoints];
        var otherLength = other.DistanceMatrix[2, otherElementPoints];
        if (Math.Abs(currentLength - otherLength) > 10)
        {
            return false;
        }
        var isFirstAngleLengthLess = AngleMatrix.GetLength(1) < other.AngleMatrix.GetLength(1);
        var firstRowIndex = isFirstAngleLengthLess ? currentElementPoints : otherElementPoints;
        var secondRowIndex = isFirstAngleLengthLess ? otherElementPoints : currentElementPoints;
        var firstMatrix = isFirstAngleLengthLess ? AngleMatrix : other.AngleMatrix;
        var secondMatrix = isFirstAngleLengthLess ? other.AngleMatrix : AngleMatrix;
        for (var i = 0; i < firstMatrix.GetLength(1); i++)
        {
            var hasFound = false;
            for (var j = 0; j < secondMatrix.GetLength(1); j++)
            {
                if (Math.Abs(firstMatrix[firstRowIndex - 1, i] - secondMatrix[secondRowIndex - 1, j]) > Angle.FromRadians(Constants.PiOver2 / 3).Degrees)
                {
                    continue;
                }
                hasFound = true;
                break;
            }
            if (!hasFound)
            {
                return false;
            }
        }
        return true;
    }

    public int GetIntersectedPointsCount(ClusterDescriptor other)
    {
        var maxCoincidence = 0;
        for (var i = 0; i < AngleMatrix.GetLength(0); i++)
        {
            CyclicShiftRight();
            for (var j = 0; j < other.AngleMatrix.GetLength(0); j++)
            {
                other.CyclicShiftRight();
                if (!AreFirstDescriptorPointsEqual(other))
                {
                    continue;
                }

                var currentCoincidence = 1;
                var lessLength = DistanceMatrix.GetLength(1) < other.DistanceMatrix.GetLength(1)
                    ? DistanceMatrix.GetLength(1)
                    : other.DistanceMatrix.GetLength(1);
                for (var k = 2; k < lessLength; k++)
                {
                    if (ArePointsEqual(other, k))
                    {
                        currentCoincidence++;
                    }
                }
                if (currentCoincidence > maxCoincidence)
                {
                    maxCoincidence = currentCoincidence;
                }
            }
        }

        return maxCoincidence;
    }
}