using POC.Models;

namespace POC.Services;

public interface IClusteringVerificationService
{
    bool Verify(IEnumerable<Cluster> first, IEnumerable<Cluster> second);
}

public sealed class ForelClusteringVerificationService : IClusteringVerificationService
{
    public bool Verify(IEnumerable<Cluster> first, IEnumerable<Cluster> second)
    {
        var firstAttempt = first
            .OrderByDescending(x => x.Minutiae.Count())
            .ToList();
        var secondAttempt = second
            .OrderByDescending(x => x.Minutiae.Count())
            .ToList();
        if (firstAttempt.Count != secondAttempt.Count)
        {
            return false;
        }
        var firstAttemptCentroids = firstAttempt.Select(x => x.Centroid).ToList();
        var secondAttemptCentroids = secondAttempt.Select(x => x.Centroid).ToList();
        return firstAttemptCentroids.All(x => secondAttemptCentroids.Contains(x));
    }
}