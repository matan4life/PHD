using MathNet.Spatial.Euclidean;
using MathNet.Spatial.Units;

namespace Presentation.Console.Extensions;

public static class Point2DExtensions
{
    public static Angle ToPolarAngle(this Point2D point, Point2D center) =>
        Angle.FromRadians(Math.Atan2(point.Y - center.Y, point.X - center.X));
}