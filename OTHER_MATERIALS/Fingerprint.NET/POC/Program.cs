using System.Drawing;
using Microsoft.Extensions.DependencyInjection;
using POC.Extensions;
using POC.Python;
using POC.Services;

static void PrintMatrix(double[,] matrix)
{
    int rows = matrix.GetLength(0);
    int cols = matrix.GetLength(1);

    // Find the maximum width of each column
    int[] columnWidths = new int[cols];
    for (int j = 0; j < cols; j++)
    {
        int maxWidth = 0;
        for (int i = 0; i < rows; i++)
        {
            int width = matrix[i, j].ToString("F2").Length;
            maxWidth = Math.Max(maxWidth, width);
        }

        columnWidths[j] = maxWidth;
    }

    // Print the matrix
    for (int i = 0; i < rows; i++)
    {
        for (int j = 0; j < cols; j++)
        {
            Console.Write(matrix[i, j].ToString("F2").PadLeft(columnWidths[j]) + " | ");
        }

        Console.WriteLine();
    }

    Console.WriteLine();
}

var serviceCollection = new ServiceCollection().AddPocServices();
var serviceProvider = serviceCollection.BuildServiceProvider();
var pythonExecutor = serviceProvider.GetRequiredService<IPythonExecutor>();
var imageSizeExtractor = serviceProvider.GetRequiredService<IImageSizeExtractor>();
var clusteringService = serviceProvider.GetRequiredService<IClusteringService>();
var verificationService = serviceProvider.GetRequiredService<IClusteringVerificationService>();
var qualityService = serviceProvider.GetRequiredService<IQualityEvaluator>();
const string imagePath = @"C:\Users\yurii\Downloads\DB1_B\101_1.tif";
var minutiae = (await pythonExecutor.ExtractMinutiaFromImageAsync(imagePath)).ToList();
var result = await clusteringService.ClusterAsync(minutiae, 40);
var clusters = result.ToList();
List<Pen> pens = [Pens.Chartreuse, Pens.Magenta, Pens.DarkGoldenrod];
for (int f = 2; f < 9; f++)
{
    string imagePath2 = @$"C:\Users\yurii\Downloads\DB1_B\101_{f}.tif";
    var otherMinutiae = (await pythonExecutor.ExtractMinutiaFromImageAsync(imagePath2)).ToList();
    var otherResult = await clusteringService.ClusterAsync(otherMinutiae, 40);
    var otherClusters = otherResult.ToList();
    // var descriptorsResult = new double[clusters.Count, otherClusters.Count];
    // for (var i = 0; i < clusters.Count; i++)
    // {
    //     for (var j = 0; j < otherClusters.Count; j++)
    //     {
    //         var firstDescriptor = ClusterDescriptor.Create(clusters[i]);
    //         var secondDescriptor = ClusterDescriptor.Create(otherClusters[j]);
    //         descriptorsResult[i, j] = firstDescriptor.GetIntersectedPointsCount(secondDescriptor);
    //     }
    // }

    // PrintMatrix(descriptorsResult);
    var otherImage = (Bitmap)Image.FromFile(imagePath2);
    var otherTemporaryBitmap = new Bitmap(388, 374);
    var otherGraphics = Graphics.FromImage(otherTemporaryBitmap);
    otherGraphics.DrawImage(otherImage, 0, 0, 388, 374);
    foreach (var (cluster, index) in otherClusters.Select((cluster, i) => (cluster, i)))
    {
        var centroid = cluster.Centroid;
        otherGraphics.FillEllipse(Brushes.Red, (float)centroid.X - 2, (float)centroid.Y - 2, 4, 4);
        var pen = new Pen(Brushes.Green, 2);
        otherGraphics.DrawEllipse(pen, (float)centroid.X - cluster.Radius, (float)centroid.Y - cluster.Radius, cluster.Radius * 2, cluster.Radius * 2);
        
        foreach (var minutia in cluster.Minutiae)
        {
            // otherGraphics.FillEllipse(Brushes.Blue, (float)minutia.X - 2, (float)minutia.Y - 2, 4, 4);
            otherGraphics.DrawLine(pens[index], centroid.X, centroid.Y, minutia.X,
                (float)minutia.Y);
        }
    }

    otherTemporaryBitmap.Save($@"C:\Users\yurii\Downloads\result{f}.png");
}
var image = (Bitmap)Image.FromFile(imagePath);
var temporaryBitmap = new Bitmap(388, 374);
var graphics = Graphics.FromImage(temporaryBitmap);
graphics.DrawImage(image, 0, 0, 388, 374);
foreach (var (cluster, index) in clusters.Select((cluster, i) => (cluster, i)))
{
    var centroid = cluster.Centroid;
    graphics.FillEllipse(Brushes.Red, (float)centroid.X - 2, (float)centroid.Y - 2, 4, 4);
    var pen = new Pen(Brushes.Green, 2);
    graphics.DrawEllipse(pen, (float)centroid.X - cluster.Radius, (float)centroid.Y - cluster.Radius, cluster.Radius * 2, cluster.Radius * 2);
    foreach (var minutia in cluster.Minutiae)
    {
        // graphics.FillEllipse(Brushes.Blue, (float)minutia.X - 2, (float)minutia.Y - 2, 4, 4);
        graphics.DrawLine(pens[index], centroid.X, centroid.Y, minutia.X, minutia.Y);
    }
}

temporaryBitmap.Save($@"C:\Users\yurii\Downloads\result1.png");

