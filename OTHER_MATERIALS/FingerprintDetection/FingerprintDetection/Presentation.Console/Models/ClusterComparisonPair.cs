using MathNet.Numerics;
using MathNet.Spatial.Units;
using Presentation.Console.Extensions;

namespace Presentation.Console.Models;

public sealed record ClusterComparisonPair(
    double FirstDistance,
    double SecondDistance,
    IEnumerable<Angle> FirstRelativeAngles,
    IEnumerable<Angle> SecondRelativeAngles)
{
    private const int DistanceThreshold = 10;
    private const double AngleThreshold = Constants.PiOver4;

    private double DistanceDifference => Math.Abs(FirstDistance - SecondDistance);

    public bool IsEquivalent()
    {
        if (DistanceDifference > DistanceThreshold)
        {
            return false;
        }
        
        return FirstRelativeAngles.Count() < SecondRelativeAngles.Count()
            ? FirstRelativeAngles.All(angle => SecondRelativeAngles.Any(otherAngle => (angle - otherAngle).NormalizeAngle() < Angle.FromRadians(AngleThreshold)))
            : SecondRelativeAngles.All(angle => FirstRelativeAngles.Any(otherAngle => (angle - otherAngle).NormalizeAngle() < Angle.FromRadians(AngleThreshold)));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FirstDistance, SecondDistance, FirstRelativeAngles,
            SecondRelativeAngles);
    }
}