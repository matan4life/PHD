using MathNet.Spatial.Euclidean;
using MathNet.Spatial.Units;
using POC.Extensions;

namespace POC.Models;

public sealed class CartesianMinutia
{
    public int X { get; set; }
    
    public int Y { get; set; }
    
    public double Theta { get; set; }
    
    public Point2D Point => new(X, Y);
    
    public Angle GetPolarAngle(CartesianMinutia centroid) => Point.ToPolarAngle(centroid.Point);
    
    public double DistanceTo(CartesianMinutia minutia) => Point.DistanceTo(minutia.Point);

    public override string ToString()
    {
        return $"X: {X}, Y: {Y}, Theta: {Theta}";
    }
}