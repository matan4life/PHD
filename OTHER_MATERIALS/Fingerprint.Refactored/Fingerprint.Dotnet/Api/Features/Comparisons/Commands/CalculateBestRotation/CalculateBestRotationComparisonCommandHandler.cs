using Api.Database;
using Api.Entities;
using Api.Extensions;
using Api.Models;
using EFCore.BulkExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Comparisons.Commands.CalculateBestRotation;

public class CalculateBestRotationComparisonCommandHandler(
    FingerprintContext context,
    ILogger<CalculateBestRotationComparisonCommandHandler> logger)
    : IRequestHandler<CalculateBestRotationComparisonCommand, TelemetryResponse>
{
    public async Task<TelemetryResponse> Handle(CalculateBestRotationComparisonCommand request,
        CancellationToken cancellationToken)
    {
        var startDateTime = DateTime.Now;
        var metrics = await context.Metrics.ToListAsync(cancellationToken);
        context.Database.SetCommandTimeout(TimeSpan.FromHours(1));
        var images = await context.Images
            .Include(x => x.Clusters)
            .ThenInclude(x => x.ClusterMinutiaes)
            .ThenInclude(x => x.Minutia)
            .ThenInclude(x => x.MinutiaeMetrics)
            .Where(x => x.TestRunId == request.TestRunId && x.ProcessedCorrectly == true)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
        var imagePairs = images.SelectMany(image => images
            .Where(otherImage => otherImage.Id > image.Id)
            .Select(otherImage => (image, otherImage)));
        var clusterPairs = imagePairs.SelectMany(pair => pair.image.Clusters
            .SelectMany(cluster => pair.otherImage.Clusters
                .Select(otherCluster => (cluster, otherCluster))));
        var results = await clusterPairs.ParallelSelectAsync(
            pair => CalculateBestRotationScore(pair.cluster, pair.otherCluster, metrics));
        context.Database.SetCommandTimeout(TimeSpan.FromHours(1));
        await context.BulkInsertAsync(results.SelectMany(x => x),
            cancellationToken: cancellationToken,
            bulkConfig: new BulkConfig()
            {
                BulkCopyTimeout = 0
            });
        var endDateTime = DateTime.Now;
        return new TelemetryResponse(startDateTime, endDateTime, endDateTime - startDateTime);
    }

    private static async Task<IEnumerable<ClusterComparison>> CalculateBestRotationScore(Cluster c1, Cluster c2,
        IList<Metric> metrics)
    {
        var centroid1 = c1.ClusterMinutiaes.First(x => x.IsCentroid);
        var centroid2 = c2.ClusterMinutiaes.First(x => x.IsCentroid);
        var pairs = c1.ClusterMinutiaes
            .Where(x => !x.IsCentroid)
            .SelectMany(minutia1 => c2.ClusterMinutiaes
                .Where(x => !x.IsCentroid)
                .Select(minutia2 => (minutia1, minutia2)));
        var results = await pairs.ParallelSelectAsync(
            pair => CalculateRotationScore(c1, c2, pair.minutia1, pair.minutia2, centroid1, centroid2, metrics));
        return results.SelectMany(x => x);
    }

    private static async Task<IEnumerable<ClusterComparison>> CalculateRotationScore(Cluster c1, Cluster c2,
        ClusterMinutiae cm1,
        ClusterMinutiae cm2,
        ClusterMinutiae centroid1, ClusterMinutiae centroid2, IList<Metric> metrics)
    {
        var result = new List<ClusterComparison>();
        var distanceMetric = metrics.First(x => x.Name == Metric.Distance);
        var angleMetric = metrics.First(x => x.Name == Metric.Angle);

        var distance1 = cm1.Minutia
            .MinutiaeMetrics
            .First(m => m.ClusterId == c1.Id
                        && m.MetricId == distanceMetric.Id
                        && m.OtherMinutiaId == centroid1.MinutiaId)
            .Value;
        var distance2 = cm2.Minutia
            .MinutiaeMetrics
            .First(m => m.ClusterId == c2.Id
                        && m.MetricId == distanceMetric.Id
                        && m.OtherMinutiaId == centroid2.MinutiaId)
            .Value;
        if (Math.Abs(distance1 - distance2) > distanceMetric.AcceptableThreshold)
        {
            return [];
        }
        else
        {
            result.Add(new ClusterComparison
            {
                Matches = true,
                FirstMinutiaId = cm1.Id,
                SecondMinutiaId = cm2.Id,
                LeadingFirstMinutiaId = cm1.Id,
                LeadingSecondMinutiaId = cm2.Id,
                DistanceDifference = Math.Abs(distance1 - distance2),
                AngleDifference = null
            });
        }

        var c1Minutiae = c1.ClusterMinutiaes
            .Where(x => x.Id != cm1.Id && x.Id != centroid1.Id)
            .OrderBy(x => x.Minutia.PolarAngleTo(centroid1.Minutia));
        var c2Minutiae = c2.ClusterMinutiaes
            .Where(x => x.Id != cm2.Id && x.Id != centroid2.Id)
            .OrderBy(x => x.Minutia.PolarAngleTo(centroid2.Minutia));
        var pairs = c1Minutiae.Zip(c2Minutiae,
            (m1, m2) => (m1, m2));
        var childResults = await pairs.ParallelSelectAsync(
            pair => AreMinutiaeWithinThreshold(pair.m1, pair.m2, cm1, cm2, centroid1, centroid2, distanceMetric,
                angleMetric));
        return [..result, ..childResults.Select(x => new ClusterComparison
        {
            Matches = x.Matched,
            FirstMinutiaId = x.m1.Id,
            SecondMinutiaId = x.m2.Id,
            LeadingFirstMinutiaId = cm1.Id,
            LeadingSecondMinutiaId = cm2.Id,
            DistanceDifference = x.DistanceDiff,
            AngleDifference = x.AngleDiff
        })];
    }

    private static async Task<(ClusterMinutiae m1, ClusterMinutiae m2, bool Matched, double DistanceDiff, double? AngleDiff)> AreMinutiaeWithinThreshold(
        ClusterMinutiae m1,
        ClusterMinutiae m2,
        ClusterMinutiae cm1,
        ClusterMinutiae cm2,
        ClusterMinutiae centroid1,
        ClusterMinutiae centroid2,
        Metric distanceMetric,
        Metric angleMetric)
    {
        var distance1 = m1.Minutia
            .MinutiaeMetrics
            .First(m => m.ClusterId == cm1.ClusterId
                        && m.MetricId == distanceMetric.Id
                        && m.OtherMinutiaId == centroid1.MinutiaId)
            .Value;
        var distance2 = m2.Minutia
            .MinutiaeMetrics
            .First(m => m.ClusterId == cm2.ClusterId
                        && m.MetricId == distanceMetric.Id
                        && m.OtherMinutiaId == centroid2.MinutiaId)
            .Value;
        var diff = Math.Abs(distance1 - distance2);
        if (diff > distanceMetric.AcceptableThreshold)
        {
            return (m1, m2, false, diff, null);
        }

        var angle1 = m1.Minutia
            .MinutiaeMetrics
            .First(m => m.ClusterId == cm1.ClusterId
                        && m.MetricId == angleMetric.Id
                        && m.OtherMinutiaId == cm1.MinutiaId)
            .Value;
        var angle2 = m2.Minutia
            .MinutiaeMetrics
            .First(m => m.ClusterId == cm2.ClusterId
                        && m.MetricId == angleMetric.Id
                        && m.OtherMinutiaId == cm2.MinutiaId)
            .Value;
        var angleDiff = Math.Abs(angle1 - angle2);
        return (m1, m2, angleDiff <= angleMetric.AcceptableThreshold, diff, angleDiff);
    }
}