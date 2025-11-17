using POC.Extensions;

namespace POC.Models;

public sealed class Cluster
{
    public required CartesianMinutia Centroid { get; init; }
    
    public required int Radius { get; init; }

    public required IEnumerable<CartesianMinutia> Minutiae { get; init; }

    public double[,] MapToDistanceMatrix()
    {
        var result = new double[3, Minutiae.Count() + 1];
        result[0, 0] = Centroid.X;
        result[1, 0] = Centroid.Y;
        result[2, 0] = 0;
        foreach (var (minutia, index) in Minutiae.Select((x, i) => (x, i + 1)))
        {
            result[0, index] = minutia.X;
            result[1, index] = minutia.Y;
            result[2, index] = minutia.DistanceTo(Centroid);
        }

        return result;
    }

    public double[,] MapToAnglesMatrix()
    {
        var count = Minutiae.Count();
        var result = new double[count, count - 1];
        for (var i = 0; i < count; i++)
        {
            for (var j = i + 1; j < count; j++)
            {
                result[i, j - i - 1] = (Minutiae.ElementAt(j).GetPolarAngle(Centroid).Normalize() -
                                        Minutiae.ElementAt(i).GetPolarAngle(Centroid).Normalize())
                    .Normalize()
                    .Degrees;
            }

            for (var j = 0; j < i; j++)
            {
                result[i, count - i - 1 + j] = (Minutiae.ElementAt(j).GetPolarAngle(Centroid).Normalize() -
                                                Minutiae.ElementAt(i).GetPolarAngle(Centroid).Normalize())
                    .Normalize()
                    .Degrees;
            }
        }

        return result;
    }
}