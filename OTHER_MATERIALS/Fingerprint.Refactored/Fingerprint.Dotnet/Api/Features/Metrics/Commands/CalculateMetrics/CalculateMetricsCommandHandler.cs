using Api.Database;
using Api.Entities;
using Api.Extensions;
using Api.Models;
using EFCore.BulkExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Metrics.Commands.CalculateMetrics;

public sealed class CalculateMetricsCommandHandler(FingerprintContext context)
    : IRequestHandler<CalculateMetricsCommand, TelemetryResponse>
{
    public async Task<TelemetryResponse> Handle(CalculateMetricsCommand request, CancellationToken cancellationToken)
    {
        var date = DateTime.Now;
        var groupedClusterMinutiae = await context.ClusterMinutiae
            .Include(x => x.Cluster)
            .ThenInclude(x => x.Image)
            .Include(x => x.Minutia)
            .Where(x => x.Cluster.Image.TestRunId == request.TestRunId)
            .GroupBy(x => x.ClusterId)
            .ToListAsync(cancellationToken);
        var metrics = await context.Metrics.ToListAsync(cancellationToken);
        var clusterMetrics = await groupedClusterMinutiae
            .ParallelSelectAsync(clusterMinutiae => CalculateMetrics(clusterMinutiae.ToList(), metrics));
        await context.BulkInsertAsync(clusterMetrics.SelectMany(x => x), cancellationToken: cancellationToken);
        var endDate = DateTime.Now;
        return new TelemetryResponse(date, endDate, endDate - date);
    }

    private async Task<IEnumerable<MinutiaeMetric>> CalculateMetrics(IList<ClusterMinutiae> minutiae,
        IList<Metric> metrics)
    {
        var centroid = minutiae.First(minutia => minutia.IsCentroid);
        var distanceMetricId = metrics.Single(x => x.Name == Metric.Distance).Id;
        var angleMetricId = metrics.Single(x => x.Name == Metric.Angle).Id;
        var distanceMetrics = await minutiae.Except([centroid])
            .ParallelSelectAsync(minutia => CalculateDistanceMetric(minutia, centroid, distanceMetricId));
        var anglePairs = minutiae.Except([centroid])
            .SelectMany(minutia => minutiae.Except([centroid])
                .Where(otherMinutia => otherMinutia.MinutiaId > minutia.MinutiaId)
                .Select(otherMinutia => (minutia, otherMinutia)));
        var angleMetrics = await anglePairs
            .ParallelSelectAsync(pair =>
                CalculateAngleMetric(pair.minutia, pair.otherMinutia, centroid, angleMetricId));
        return distanceMetrics.Concat(angleMetrics.SelectMany(x => x));
    }

    private async Task<MinutiaeMetric> CalculateDistanceMetric(ClusterMinutiae minutia, ClusterMinutiae centroid,
        int distanceMetricId)
    {
        return new MinutiaeMetric
        {
            MetricId = distanceMetricId,
            MinutiaId = minutia.MinutiaId,
            OtherMinutiaId = centroid.MinutiaId,
            ClusterId = minutia.ClusterId,
            Value = minutia.Minutia.DistanceTo(centroid.Minutia)
        };
    }

    private async Task<IEnumerable<MinutiaeMetric>> CalculateAngleMetric(ClusterMinutiae minutia,
        ClusterMinutiae otherMinutia,
        ClusterMinutiae centroid, int metricId)
    {
        var angle = Math.Atan2(minutia.Minutia.Y - centroid.Minutia.Y, minutia.Minutia.X - centroid.Minutia.X) -
                    Math.Atan2(otherMinutia.Minutia.Y - centroid.Minutia.Y,
                        otherMinutia.Minutia.X - centroid.Minutia.X);
        var normalizedAngle = angle < 0 ? angle + 2 * Math.PI : angle;

        return
        [
            new MinutiaeMetric
            {
                MetricId = metricId,
                MinutiaId = minutia.MinutiaId,
                OtherMinutiaId = otherMinutia.MinutiaId,
                ClusterId = minutia.ClusterId,
                Value = normalizedAngle
            },
            new MinutiaeMetric
            {
                MetricId = metricId,
                MinutiaId = otherMinutia.MinutiaId,
                OtherMinutiaId = minutia.MinutiaId,
                ClusterId = minutia.ClusterId,
                Value = 2 * Math.PI - normalizedAngle
            }
        ];
    }
}