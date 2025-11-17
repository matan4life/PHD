using System.Drawing;
using MediatR;

namespace Presentation.Console.Features.VisualizeClusters;

public sealed class VisualizeClustersCommandHandler : IRequestHandler<VisualizeClustersCommand>
{
    private IEnumerable<Color> ClusterColors => [Color.Red, Color.Green, Color.Blue];
    
    public async Task Handle(VisualizeClustersCommand request, CancellationToken cancellationToken)
    {
        using var bitmap = (Bitmap)Image.FromFile(request.ImagePath);
        using var emptyBitmap = new Bitmap(bitmap.Width, bitmap.Height);
        using var graphics = Graphics.FromImage(emptyBitmap);
        graphics.DrawImage(bitmap, 0, 0);
        var clusterColors = ClusterColors.ToList();
        foreach (var cluster in request.Clusters)
        {
            var color = clusterColors[request.Clusters.ToList().IndexOf(cluster) % clusterColors.Count];
            foreach (var minutia in cluster.Minutiae)
            {
                graphics.FillEllipse(new SolidBrush(color), minutia.X, minutia.Y, 3, 3);
                graphics.DrawLine(new Pen(color), minutia.X, minutia.Y, cluster.Centroid.X, cluster.Centroid.Y);
            }
        }
        var outputImagePath = Path.Combine(Path.GetDirectoryName(request.ImagePath)!, Path.GetFileNameWithoutExtension(request.ImagePath) + "_clusters" + Path.GetExtension(request.ImagePath));
        emptyBitmap.Save(outputImagePath);
    }
}