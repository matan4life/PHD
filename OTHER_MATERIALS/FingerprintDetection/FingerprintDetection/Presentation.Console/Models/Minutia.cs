using MathNet.Spatial.Euclidean;
using MathNet.Spatial.Units;
using Presentation.Console.Extensions;

namespace Presentation.Console.Models;

public sealed record Minutia(int X, int Y, double Theta)
{
    public Point2D EuclideanPoint => new(X, Y);

    public Angle PolarAngleFromCenter(Minutia center) => EuclideanPoint.ToPolarAngle(center.EuclideanPoint);
    
    public double DistanceTo(Minutia other) => EuclideanPoint.DistanceTo(other.EuclideanPoint);
}