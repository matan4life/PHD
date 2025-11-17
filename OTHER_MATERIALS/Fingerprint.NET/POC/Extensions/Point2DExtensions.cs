using MathNet.Spatial.Euclidean;
using MathNet.Spatial.Units;

namespace POC.Extensions;

public static class Point2DExtensions
{
    public static Angle ToPolarAngle(this Point2D point, Point2D centroid)
    {
        return Angle.FromRadians(Math.Atan2(point.Y - centroid.Y, point.X - centroid.X));
    }
    
    public static Point2D GetClusterCenter(this IEnumerable<Point2D> points)
    {
        var point2Ds = points.ToList();
        var distances = point2Ds.Select(x => new
        {
            Key = x,
            Value = point2Ds.Select(y => x.DistanceTo(y)).Sum()
        });
        return distances.OrderBy(x => x.Value).First().Key;
    }
}