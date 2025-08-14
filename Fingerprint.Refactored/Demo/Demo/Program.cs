// See https://aka.ms/new-console-template for more information
using Demo;
using Demo.Database;
using Microsoft.EntityFrameworkCore;
using System.Text;

var context = new FingerprintContext();

var images = await context.Images
    .Where(x => x.TestRunId == 1)
    .Include(x => x.Minutiae)
    .AsNoTracking()
    .ToListAsync();

var minutiae = images.SelectMany(x => x.Minutiae).ToList();

var sw = new System.Diagnostics.Stopwatch();
sw.Start();
//foreach (var minutia in minutiae)
//{
//    foreach (var otherMinutia in minutiae)
//    {
//        if (minutia.Id == otherMinutia.Id)
//            continue;
//        result.Add(new(minutia.ImageId, otherMinutia.ImageId, minutia.Id, otherMinutia.Id, 0));
//    }
//}
var sf = new ParallellStepFunction(minutiae);
sf.AdjustCoords();
sf.CalculateMetrics();
sf.MakeLocalComparison();
sf.MakeGlobalComparison();
sw.Stop();
Console.WriteLine($"Elapsed time: {sw.ElapsedMilliseconds} ms");
//var sf = new ParallellStepFunctionV2(minutiae);
//sf.AdjustCoords();
//sf.CalculateMetrics();
//sf.MakeLocalComparison();
//var sb = new StringBuilder("image_id_1,image_id_2,minutia_id_1,minutia_id_2,score\n");
//foreach (var result in sf.LocalResults.OrderBy(x => x.ImageId1).ThenByDescending(x => x.Score))
//{
//    sb.AppendLine($"{result.ImageId1},{result.ImageId2},{result.TargetMinutiaId1},{result.TargetMinutiaId2},{result.Score:F2}");
//}
//await File.WriteAllTextAsync($"results_local.csv", sb.ToString());
var ids = sf.GlobalResults.Select(x => x.ImageId1).Distinct().Order();
var sb = new StringBuilder()
    .AppendLine($",{string.Join(",", ids)}");
var minIndex = images.Min(x => x.Id);
var maxIndex = images.Max(x => x.Id);
foreach (var id in ids)
{
    sb.Append($"{id},");
    var idResults = sf.GlobalResults.Where(x => x.ImageId1 == id).Append(new(id, id, 100, 0, 0));
    var adjustedResults = Enumerable.Range(minIndex, maxIndex - minIndex + 1).Select(i => idResults.FirstOrDefault(x => x.ImageId2 == i) ?? new GlobalComparisonResult(id, i, 0, 0, 0));
    sb.AppendLine(string.Join(",", adjustedResults.OrderBy(x => x.ImageId2).Select(x => $"{x.Score:F2}%")));
}

await File.WriteAllTextAsync($"results.csv", sb.ToString());
//var results = new Dictionary<(int ImageId1, int ImageId2), double>();
//for (var i = 0; i < images.Count; i++)
//{
//    var vector1 = FastSearch.ComputeFeatureVector(images[i]);
//    for (var j = i + 1; j < images.Count; j++)
//    {
//        var vector2 = FastSearch.ComputeFeatureVector(images[j]);
//        var score = FastSearch.Compare(vector1, vector2);
//        if (i / 8 == j / 8)
//        {
//            Console.ForegroundColor = ConsoleColor.Green;
//        }
//        else
//        {
//            Console.ForegroundColor = ConsoleColor.Red;
//        }scor
//        Console.WriteLine($"Score for {i} and {j}: {e}");
//    }
//}

record LocalComparisonResult(int ImageId1, int ImageId2, int TargetMinutiaId1, int TargetMinutiaId2, double Score);

record GlobalComparisonResult(int ImageId1, int ImageId2, double Score, int MinutiaId1, int MinutiaId2);

record MinutiaComparisonResult(int MinutiaId1, int MinutiaId2, double Score);
