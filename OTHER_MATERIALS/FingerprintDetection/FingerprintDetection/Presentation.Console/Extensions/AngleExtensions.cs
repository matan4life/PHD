using MathNet.Numerics;
using MathNet.Spatial.Units;

namespace Presentation.Console.Extensions;

public static class AngleExtensions
{
    public static Angle NormalizeAngle(this Angle angle)
        => Angle.FromDegrees(Euclid.Modulus(angle.Degrees, 360.0));
}