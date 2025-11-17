using Dumpify;
using MediatR;
using Presentation.Console.Extensions;
using Presentation.Console.Models;
using VectSharp;
using ColorCombination = (VectSharp.Colour CenterColor, VectSharp.Colour PathColor);

namespace Presentation.Console.EventSourcing.Events.ClustersSeparated;

public sealed class ClustersSeparatedNotificationHandler(IEventStore eventStore)
    : INotificationHandler<ClustersSeparatedNotification>
{
    public async Task Handle(ClustersSeparatedNotification notification, CancellationToken cancellationToken)
    {
        var page = new Page(1000, 1500);
        DrawHeader(page, notification.FileName);
        DrawClusters(page, notification.Clusters.Select(c => c.Item1));
        DrawMatrices(page, notification.Clusters.Select(c => c.Item2));
        var @event = new ClustersSeparatedEvent(notification.AggregateId, page);
        await eventStore.SaveEventAsync(@event);
    }

    private void DrawHeader(Page page, string fileName)
    {
        var fontFamily = FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica);
        var font = new Font(fontFamily, 36, underlined: true);
        var text = $"{fileName} clusters distribution";
        var textSize = font.MeasureText(text);
        var strokeColor = Colour.FromRgb(0, 80, 44);
        var fillColor = Colour.FromRgb(0, 178, 115);
        var startPoint = new Point(500 - textSize.Width / 2, 20);
        page.Graphics.StrokeText(startPoint, text, font, strokeColor, TextBaselines.Middle,
            lineJoin: LineJoins.Round);
        page.Graphics.FillText(startPoint, text, font, fillColor, TextBaselines.Middle);
    }

    private void DrawClusters(Page page, IEnumerable<Cluster> clusters)
    {
        var graphics = page.Graphics;
        IList<ColorCombination> colorCombinations =
        [
            new ColorCombination(Colours.Red, Colours.Green),
            new ColorCombination(Colours.YellowGreen, Colours.Violet),
            new ColorCombination(Colours.Blue, Colours.Orange)
        ];
        IList<int> relativeCenters = [200, 500, 800];
        foreach (var (cluster, index) in clusters.Select((c, i) => (c, i)))
        {
            var (borderColor, centerColor) = colorCombinations[index];
            var text = $"Cluster # {index + 1}";
            var fontFamily = FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica);
            var font = new Font(fontFamily, 20);
            var textSize = font.MeasureText(text);
            var currentCenterX = relativeCenters[index];
            graphics.FillText(
                new Point(-textSize.Width / 2, currentCenterX - cluster.Radius - textSize.Height - 15),
                text,
                font,
                Colours.Black);
            foreach (var minutia in cluster.Minutiae)
            {
                var deltaX = minutia.X - cluster.Centroid.X;
                var deltaY = minutia.Y - cluster.Centroid.Y;
                var point = new Point(deltaX, currentCenterX + deltaY);
                var linePath = new GraphicsPath();
                linePath.MoveTo(new Point(0, currentCenterX)).LineTo(point);
                graphics.StrokePath(linePath, borderColor);
                var pointPath = new GraphicsPath();
                pointPath.Arc(point, 2, 0, 2 * Math.PI);
                graphics.FillPath(pointPath, Colours.Black);
            }

            var centerPath = new GraphicsPath();
            centerPath.Arc(new Point(0, currentCenterX), 2, 0, 2 * Math.PI);
            graphics.FillPath(centerPath, centerColor);
            var borderPath = new GraphicsPath();
            borderPath.Arc(new Point(0, currentCenterX), cluster.Radius, 0, 2 * Math.PI);
            graphics.StrokePath(borderPath, borderColor);
        }
    }

    private void DrawMatrices(Page page, IEnumerable<ClusterDescriptor> descriptors)
    {
        var graphics = page.Graphics;
        IList<int> distanceRelativeCenters = [150, 450, 750];
        IList<int> angleRelativeCenters = [150, 550, 950];
        foreach (var (descriptor, index) in descriptors.Select((d, i) => (d, i)))
        {
            const string distanceDescriptorHeader = "Distance matrix";
            const string angleDescriptorHeader = "Angle matrix";
            var headerFontFamily = FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica);
            var headerFont = new Font(headerFontFamily, 20);
            var headerTextSize = headerFont.MeasureText(distanceDescriptorHeader);
            var angleHeaderSize = headerFont.MeasureText(angleDescriptorHeader);
            var textFontFamily = FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica);
            var textFont = new Font(textFontFamily, 10);
            var currentCenterX = distanceRelativeCenters[index];
            var angleCenterX = angleRelativeCenters[index];
            graphics.FillText(
                new Point(150, currentCenterX - headerTextSize.Height - 15),
                distanceDescriptorHeader,
                headerFont,
                Colours.Black);
            var distanceDescriptor = descriptor.DistanceDescriptor;
            var output = distanceDescriptor.ToMatrixString(15, 15, "F1").Split('\n');
            for (var row = 0; row < distanceDescriptor.RowCount; row++)
            {
                var matrixRow = output[row];
                var path = new GraphicsPath();
                path.MoveTo(150, currentCenterX + (row + 1) * 20)
                    .LineTo(600, currentCenterX + (row + 1) * 20);
                graphics.FillTextOnPath(path, matrixRow, textFont, Colours.Black);
            }
            
            graphics.FillText(
                new Point(700, angleCenterX - angleHeaderSize.Height - 15),
                angleDescriptorHeader,
                headerFont,
                Colours.Black);
            
            var anglesDescriptor = descriptor.AnglesDescriptor;
            var anglesOutput = anglesDescriptor.ToMatrixString(225, 225, "F1").Split('\n');
            for (var row = 0; row < anglesDescriptor.RowCount; row++)
            {
                var matrixRow = anglesOutput[row];
                var path = new GraphicsPath();
                path.MoveTo(700, angleCenterX + (row + 1) * 20)
                    .LineTo(1150, angleCenterX + (row + 1) * 20);
                graphics.FillTextOnPath(path, matrixRow, textFont, Colours.Black);
            }
        }
    }
}