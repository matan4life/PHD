using System.Text.Json;
using MathNet.Spatial.Units;
using Presentation.Console.Extensions;
using Presentation.Console.Models;

namespace Presentation.Console.Services;

public interface IDescriptorComparator
{
    int Compare(string sessionId, ClusterDescriptor descriptor1, ClusterDescriptor descriptor2, int firstIndex, int secondIndex);
}

public sealed class DescriptorComparator : IDescriptorComparator
{
    private const double DistanceThreshold = 10;
    private const double AngleThreshold = double.Pi / 4;

    private readonly JsonSerializerOptions _options = new()
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public int Compare(string sessionId, ClusterDescriptor descriptor1, ClusterDescriptor descriptor2, int firstIndex, int secondIndex)
    {
        Directory.CreateDirectory(
            $@"D:\PHD\FingerprintDetection\Telemetry\{sessionId}\{firstIndex}_{secondIndex}");
        if (!File.Exists(@$"D:\PHD\FingerprintDetection\Telemetry\{sessionId}\0_{firstIndex}.json"))
        {
            using var stream =
                new FileStream(@$"D:\PHD\FingerprintDetection\Telemetry\{sessionId}\0_{firstIndex}.json",
                    FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            JsonSerializer.Serialize(stream, new
            {
                Distances = descriptor1.DistanceDescriptor.ToColumnArrays(),
                Angles = descriptor1.AnglesDescriptor.ToColumnArrays(),
                Center = new
                {
                    descriptor1.Cluster.Centroid.X, descriptor1.Cluster.Centroid.Y
                }
            }, _options);
        }

        if (!File.Exists(@$"D:\PHD\FingerprintDetection\Telemetry\{sessionId}\1_{secondIndex}.json"))
        {
            using var stream =
                new FileStream(@$"D:\PHD\FingerprintDetection\Telemetry\{sessionId}\1_{secondIndex}.json",
                    FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            JsonSerializer.Serialize(stream, new
            {
                Distances = descriptor2.DistanceDescriptor.ToColumnArrays(),
                Angles = descriptor2.AnglesDescriptor.ToColumnArrays(),
                Center = new
                {
                    descriptor2.Cluster.Centroid.X, descriptor2.Cluster.Centroid.Y
                }
            }, _options);
        }

        var shiftIndices = new ShiftedIndexSequence(descriptor1.DistanceDescriptor.ColumnCount);
        var otherShiftIndices = new ShiftedIndexSequence(descriptor2.DistanceDescriptor.ColumnCount);
        var maxCompareScore = 0;
        for (var firstPosition = 0; firstPosition < shiftIndices.Count(); firstPosition++)
        {
            shiftIndices.ShiftRight();
            var initVectorIndex = shiftIndices.First();
            var initVectorLength = descriptor1.DistanceDescriptor[2, initVectorIndex];
            for (var secondPosition = 0; secondPosition < otherShiftIndices.Count(); secondPosition++)
            {
                otherShiftIndices.ShiftRight();
                using var stream = new FileStream(
                    $@"D:\PHD\FingerprintDetection\Telemetry\{sessionId}\{firstIndex}_{secondIndex}\{firstPosition}_{secondPosition}.json",
                    FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                var telemetry = new List<Telemetry>();
                var otherInitVectorIndex = otherShiftIndices.First();
                var otherInitVectorLength = descriptor2.DistanceDescriptor[2, otherInitVectorIndex];
                if (Math.Abs(initVectorLength - otherInitVectorLength) > DistanceThreshold)
                {
                    telemetry.Add(new Telemetry(false,
                        initVectorIndex,
                        otherInitVectorIndex,
                        "Init vector length",
                        initVectorLength,
                        otherInitVectorLength,
                        0,
                        maxCompareScore,
                        $"Init vectors distance threshold exceeded. Acceptable threshold: {DistanceThreshold}. Current distance difference: {Math.Abs(initVectorLength - otherInitVectorLength):F2}"));
                    JsonSerializer.Serialize(stream, new {
                        telemetry
                    }, _options);
                    continue;
                }

                telemetry.Add(new Telemetry(true,
                    initVectorIndex,
                    otherInitVectorIndex,
                    "Init vector length",
                    initVectorLength,
                    otherInitVectorLength,
                    0,
                    maxCompareScore,
                    $"Init vectors distance threshold passed. Acceptable threshold: {DistanceThreshold}. Current distance difference: {Math.Abs(initVectorLength - otherInitVectorLength):F2}"));

                var compareScore = 0;
                var firstIndices = new List<int>();
                var secondIndices = new List<int>();
                foreach (var index in shiftIndices.Skip(1).Where(i => i != initVectorIndex))
                {
                    if (firstIndices.Contains(index))
                    {
                        continue;
                    }

                    var indexSearchPair = (descriptor1.DistanceDescriptor[2, index],
                        descriptor1.AnglesDescriptor[initVectorIndex, index]);
                    foreach (var otherIndex in otherShiftIndices.Skip(1).Where(i => i != otherInitVectorIndex))
                    {
                        if (firstIndices.Contains(index) || secondIndices.Contains(otherIndex))
                        {
                            continue;
                        }

                        var otherSearchPair = (descriptor2.DistanceDescriptor[2, otherIndex],
                            descriptor2.AnglesDescriptor[otherInitVectorIndex, otherIndex]);
                        var distanceDiff = Math.Abs(indexSearchPair.Item1 - otherSearchPair.Item1);
                        var angleDiff = Angle.FromDegrees(otherSearchPair.Item2) -
                                        Angle.FromDegrees(indexSearchPair.Item2);
                        if (distanceDiff > DistanceThreshold)
                        {
                            telemetry.Add(new Telemetry(false,
                                index,
                                otherIndex,
                                "Distance",
                                indexSearchPair.Item1,
                                otherSearchPair.Item1,
                                compareScore,
                                maxCompareScore,
                                $"Distance threshold exceeded. Acceptable threshold: {DistanceThreshold}. Current distance difference: {distanceDiff:F2}"));
                            continue;
                        }

                        telemetry.Add(new Telemetry(true,
                            index,
                            otherIndex,
                            "Distance",
                            indexSearchPair.Item1,
                            otherSearchPair.Item1,
                            compareScore,
                            maxCompareScore,
                            $"Distance threshold passed. Acceptable threshold: {DistanceThreshold}. Current distance difference: {distanceDiff:F2}"));
                        if (angleDiff.NormalizeAngle() > Angle.FromRadians(AngleThreshold))
                        {
                            telemetry.Add(new Telemetry(false,
                                index,
                                otherIndex,
                                "Angle",
                                indexSearchPair.Item2,
                                otherSearchPair.Item2,
                                compareScore,
                                maxCompareScore,
                                $"Angle threshold exceeded. Acceptable threshold: {Angle.FromRadians(AngleThreshold).Degrees:F2}. Current angle difference: {angleDiff.Degrees:F2}"));
                            continue;
                        }

                        telemetry.Add(new Telemetry(true,
                            index,
                            otherIndex,
                            "Angle",
                            indexSearchPair.Item2,
                            otherSearchPair.Item2,
                            compareScore,
                            maxCompareScore,
                            $"Angle threshold passed. Acceptable threshold: {Angle.FromRadians(AngleThreshold).Degrees:F2}. Current angle difference: {angleDiff.Degrees:F2}"));
                        compareScore++;
                        telemetry.Add(new Telemetry(true,
                            index,
                            otherIndex,
                            "Score",
                            null,
                            null,
                            compareScore,
                            maxCompareScore,
                            $"Retrieved compare score: {compareScore}"));
                        firstIndices.Add(index);
                        secondIndices.Add(otherIndex);
                    }
                }

                if (compareScore > maxCompareScore)
                {
                    telemetry.Add(new Telemetry(true,
                        null,
                        null,
                        "Max score",
                        null,
                        null,
                        compareScore,
                        maxCompareScore,
                        $"Retrieved max compare score: {maxCompareScore}"));
                    maxCompareScore = compareScore;
                }

                JsonSerializer.Serialize(stream, new
                {
                    telemetry,
                    firstIndices,
                    secondIndices,
                    maxCompareScore
                }, _options);
            }
        }

        return maxCompareScore;
    }
}