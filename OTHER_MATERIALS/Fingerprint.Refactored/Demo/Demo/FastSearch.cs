using Demo.Entities;

namespace Demo;

static class FastSearch
{
    private const int MCC_HISTOGRAM_SIZE = 128; // 7-bit MCC codes (0-127)
    private const double R_MAX = 75.0; // Maximum radius for MCC neighborhood
    private const int ANGULAR_SECTORS = 8; // 45-degree sectors
    private const int RADIAL_BANDS = 2; // Inner and outer bands

    public static double[] ComputeFeatureVector(Image image)
    {
        var minutiae = image.Minutiae.ToList();

        // MCC Histogram (128 elements)
        double[] mccHistogram = ComputeMCCHistogram(minutiae);

        // Global Features (10 elements)
        double[] globalFeatures = ComputeGlobalFeatures(minutiae);

        // Combine into one vector
        return [.. mccHistogram, .. globalFeatures];
    }

    private static double[] ComputeMCCHistogram(List<Minutia> minutiae)
    {
        int[] histogram = new int[MCC_HISTOGRAM_SIZE];
        foreach (var reference in minutiae)
        {
            int mccCode = ComputeMCC(reference, minutiae);
            histogram[mccCode]++;
        }

        // Normalize histogram by number of minutiae
        double[] normalizedHistogram = [.. histogram.Select(count => (double)count / minutiae.Count)];
        return normalizedHistogram;
    }

    private static int ComputeMCC(Minutia reference, List<Minutia> minutiae)
    {
        int[] sectorCounts = new int[ANGULAR_SECTORS * RADIAL_BANDS];
        foreach (var neighbor in minutiae)
        {
            if (ReferenceEquals(reference, neighbor)) continue;

            double dx = neighbor.X - reference.X;
            double dy = neighbor.Y - reference.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance > R_MAX) continue;

            // Compute relative angle adjusted by reference minutia's angle
            double angle = Math.Atan2(dy, dx) - reference.Theta;
            if (angle < 0) angle += 2 * Math.PI;

            // Map to sector and band
            int sector = (int)(angle / (2 * Math.PI / ANGULAR_SECTORS)) % ANGULAR_SECTORS;
            int band = distance < (R_MAX / 2) ? 0 : 1;
            int index = sector + band * ANGULAR_SECTORS;

            sectorCounts[index]++;
        }

        // Simplified MCC: Convert counts to a 7-bit code (example mapping)
        int code = 0;
        for (int i = 0; i < Math.Min(7, sectorCounts.Length); i++)
            if (sectorCounts[i] > 0) code |= (1 << i);

        return code;
    }

    private static double[] ComputeGlobalFeatures(List<Minutia> minutiae)
    {
        int count = minutiae.Count;
        double meanX = minutiae.Average(m => m.X);
        double meanY = minutiae.Average(m => m.Y);
        double stdX = Math.Sqrt(minutiae.Average(m => Math.Pow(m.X - meanX, 2)));
        double stdY = Math.Sqrt(minutiae.Average(m => Math.Pow(m.Y - meanY, 2)));

        // Angles in sine/cosine for rotation invariance
        double meanCosAngle = minutiae.Average(m => Math.Cos(m.Theta));
        double meanSinAngle = minutiae.Average(m => Math.Sin(m.Theta));
        double stdCosAngle = Math.Sqrt(minutiae.Average(m => Math.Pow(Math.Cos(m.Theta) - meanCosAngle, 2)));
        double stdSinAngle = Math.Sqrt(minutiae.Average(m => Math.Pow(Math.Sin(m.Theta) - meanSinAngle, 2)));

        double propBifurcation = minutiae.Count(m => m.IsTermination == 0) / (double)count;
        double propTermination = minutiae.Count(m => m.IsTermination == 1) / (double)count;

        return
        [
                meanX, stdX, meanY, stdY,
                meanCosAngle, stdCosAngle, meanSinAngle, stdSinAngle,
                propBifurcation, propTermination
        ];
    }

    public static double Compare(double[] vector1, double[] vector2)
    {
        if (vector1.Length != vector2.Length)
            throw new ArgumentException("Feature vectors must have the same length.");

        double sumSquaredDiff = 0;
        for (int i = 0; i < vector1.Length; i++)
        {
            double diff = vector1[i] - vector2[i];
            sumSquaredDiff += diff * diff;
        }
        return Math.Sqrt(sumSquaredDiff);
    }
}
