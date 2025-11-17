using MathNet.Spatial.Units;

namespace POC.Extensions;

public static class AnglesExtensions
{
    public static Angle Normalize(this Angle angle)
    {
        var normalized = angle.Radians % (2 * Math.PI);
        return Angle.FromRadians(normalized >= 0 ? normalized : normalized + 2 * Math.PI);
    }
}