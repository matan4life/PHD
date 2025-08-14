using Demo.Entities;
using System.Collections.Concurrent;

namespace Demo;

sealed class ParallellSimdStepFunction(List<Minutia> minutiae) : IStepFunction
{
    public IEnumerable<GlobalComparisonResult> GlobalResults { get; private set; } = [];
    private List<Minutia> Minutiae { get; } = [.. minutiae];
    private Dictionary<(int Id1, int Id2), double> DistanceCache { get; } = [];
    private Dictionary<(int Id1, int Id2), double> AngleCache { get; } = [];
    private IEnumerable<IGrouping<int, Minutia>> Groups { get; set; } = [];
    private IEnumerable<LocalComparisonResult> LocalResults { get; set; } = [];
    private Dictionary<int, Minutia> MinutiaeById { get; set; } = [];
    private Dictionary<int, List<Minutia>> MinutiaeByImageId { get; set; } = [];

    public void AdjustCoords()
    {
        for (int i = 0; i < Minutiae.Count; i++)
        {
            Minutia m = Minutiae[i];
            m.X -= m.Image.WidthShift ?? 0;
            m.Y -= m.Image.HeightShift ?? 0;
        }

        var squares = GetSquares();
        Groups = GetMinutiaeInSquares(squares);
        MinutiaeById = Minutiae.ToDictionary(m => m.Id);
        MinutiaeByImageId = Minutiae.GroupBy(x => x.ImageId).ToDictionary(g => g.Key, g => g.ToList());
    }

    public void CalculateMetrics()
    {
        Parallel.ForEach(Groups, new ParallelOptions { MaxDegreeOfParallelism = 32 }, group =>
        {
            var filteredMinutiae = group.ToArray();
            int length = filteredMinutiae.Length;
            for (int i = 0; i < length; i++)
            {
                var leadMinutia = filteredMinutiae[i];
                for (int j = i + 1; j < length; j++)
                {
                    var otherMinutia = filteredMinutiae[j];
                    var deltaX = leadMinutia.X - otherMinutia.X;
                    var deltaY = leadMinutia.Y - otherMinutia.Y;

                    var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                    var angleInRads = Math.Atan2(deltaY, deltaX);
                    var angleInDegrees = ((angleInRads + 2 * Math.PI) % (2 * Math.PI)) / Math.PI * 180;

                    lock (DistanceCache) DistanceCache[(leadMinutia.Id, otherMinutia.Id)] = distance;
                    lock (AngleCache) AngleCache[(leadMinutia.Id, otherMinutia.Id)] = angleInDegrees;
                }
            }
        });
    }

    public void MakeLocalComparison()
    {
        var groups = Groups.Select(g => g.ToList()).ToList();
        var results = new ConcurrentBag<LocalComparisonResult>();
        var pairs = Enumerable.Range(0, groups.Count)
            .SelectMany(i => Enumerable.Range(i + 1, groups.Count - i - 1).Select(j => (i, j)))
            .ToList();

        Parallel.ForEach(pairs, new ParallelOptions { MaxDegreeOfParallelism = 32 }, pair =>
        {
            var group1 = groups[pair.i];
            var group2 = groups[pair.j];
            var localResults = new List<LocalComparisonResult>(group1.Count * group2.Count / 2); // Pre-size
            for (int k = 0; k < group1.Count; k++)
            {
                var minutia1 = group1[k];
                var others1 = group1.Where(x => x.Id != minutia1.Id);
                for (int l = 0; l < group2.Count; l++)
                {
                    var minutia2 = group2[l];
                    var others2 = group2.Where(x => x.Id != minutia2.Id);
                    var similarity = GetSquareSimilarity(minutia1, minutia2, others1, others2);
                    if (similarity >= 30)
                    {
                        localResults.Add(new LocalComparisonResult(minutia1.ImageId, minutia2.ImageId, minutia1.Id, minutia2.Id, similarity));
                        localResults.Add(new LocalComparisonResult(minutia2.ImageId, minutia1.ImageId, minutia2.Id, minutia1.Id, similarity));
                    }
                }
            }
            foreach (var result in localResults) results.Add(result);
        });

        LocalResults = results;
    }

    public void MakeGlobalComparison()
    {
        var groups = LocalResults.GroupBy(x => (x.ImageId1, x.ImageId2)).ToList();
        var results = new ConcurrentBag<GlobalComparisonResult>();

        Parallel.ForEach(groups, new ParallelOptions { MaxDegreeOfParallelism = 32 }, group =>
        {
            if (group.Key.ImageId1 > group.Key.ImageId2) return;

            var groupElems = group.ToList();
            var m1 = MinutiaeByImageId[group.Key.ImageId1];
            var m2 = MinutiaeByImageId[group.Key.ImageId2];
            var scores = new List<(int m1, int m2, double score)>(groupElems.Count * 62);

            foreach (var localResult in groupElems)
            {
                var shift = ShiftMinutia(group.Key.ImageId2, localResult.TargetMinutiaId1, localResult.TargetMinutiaId2);
                var globalResult = CompareGlobal(m1, shift).ToList();
                double score = globalResult.Count / (double)Math.Min(m1.Count, m2.Count) * 100;
                scores.Add((localResult.TargetMinutiaId1, localResult.TargetMinutiaId2, score));

                foreach (var result in globalResult)
                {
                    var shift1 = ShiftMinutia(group.Key.ImageId2, result.MinutiaId1, result.MinutiaId2);
                    var globalResult1 = CompareGlobal(m1, shift1).ToList();
                    double score1 = globalResult1.Count / (double)Math.Min(m1.Count, m2.Count) * 100;
                    scores.Add((result.MinutiaId1, result.MinutiaId2, score1));
                }
            }

            var maxScore = scores.MaxBy(x => x.score);
            results.Add(new GlobalComparisonResult(group.Key.ImageId1, group.Key.ImageId2, maxScore.score, maxScore.m1, maxScore.m2));
            results.Add(new GlobalComparisonResult(group.Key.ImageId2, group.Key.ImageId1, maxScore.score, maxScore.m2, maxScore.m1));
        });

        GlobalResults = results;
    }

    private Dictionary<int, (double X, double Y)> GetSquares()
    {
        var sums = new Dictionary<int, (double sumX, double sumY, int count)>(80); // Pre-size for 80 images
        foreach (var m in Minutiae)
        {
            if (!sums.TryGetValue(m.ImageId, out var data))
            {
                data = (0, 0, 0);
            }
            sums[m.ImageId] = (data.sumX + m.X, data.sumY + m.Y, data.count + 1);
        }

        var result = new Dictionary<int, (double X, double Y)>(80);
        foreach (var kvp in sums)
        {
            result[kvp.Key] = (kvp.Value.sumX / kvp.Value.count, kvp.Value.sumY / kvp.Value.count);
        }
        return result;
    }

    private IEnumerable<IGrouping<int, Minutia>> GetMinutiaeInSquares(Dictionary<int, (double X, double Y)> squares)
    {
        const int halfSize = 75;
        return Minutiae
            .GroupBy(x => x.ImageId)
            .Select(g =>
            {
                var (centerX, centerY) = squares[g.Key];
                double minX = centerX - halfSize;
                double maxX = centerX + halfSize;
                double minY = centerY - halfSize;
                double maxY = centerY + halfSize;
                return g.Where(m => m.X >= minX && m.X <= maxX && m.Y >= minY && m.Y <= maxY)
                        .AsGrouping(g.Key);
            })
            .Where(g => g.Any());
    }

    private static double GetMetric(IDictionary<(int Id1, int Id2), double> metrics, int Id1, int Id2, bool isAngle = false)
    {
        if (metrics.TryGetValue((Id1, Id2), out var metric))
        {
            return metric;
        }

        if (metrics.TryGetValue((Id2, Id1), out var reversedMetric))
        {
            return isAngle ? 360 - reversedMetric : reversedMetric;
        }

        return double.NaN;
    }

    private double GetSquareSimilarity(Minutia m1, Minutia m2, IEnumerable<Minutia> ml1, IEnumerable<Minutia> ml2)
    {
        var ml1List = ml1.ToList();
        var ml2List = ml2.ToList();
        if (ml1List.Count == 0 || ml2List.Count == 0) return 0;

        var used1 = new HashSet<int>(ml1List.Count);
        var used2 = new HashSet<int>(ml2List.Count);
        var score = 0;
        var maxMatches = Math.Min(ml1List.Count, ml2List.Count);

        var convolutions = new List<(int id1, int id2, double equality)>(ml1List.Count * ml2List.Count);
        for (int i = 0; i < ml1List.Count; i++)
        {
            for (int j = 0; j < ml2List.Count; j++)
            {
                double conv = CalculateLocalConvolution(m1, m2, ml1List[i], ml2List[j]);
                if (conv != double.PositiveInfinity)
                {
                    convolutions.Add((ml1List[i].Id, ml2List[j].Id, conv));
                }
            }
        }

        convolutions.Sort((a, b) => a.equality.CompareTo(b.equality));
        foreach (var match in convolutions)
        {
            if (score >= maxMatches) break;
            if (!used1.Contains(match.id1) && !used2.Contains(match.id2))
            {
                used1.Add(match.id1);
                used2.Add(match.id2);
                score++;
            }
        }

        return (double)score / maxMatches * 100;
    }

    private double CalculateLocalConvolution(Minutia m1, Minutia m2, Minutia m3, Minutia m4)
    {
        var distance1 = GetMetric(DistanceCache, m1.Id, m3.Id);
        var distance2 = GetMetric(DistanceCache, m2.Id, m4.Id);
        var diffDistance = Math.Abs(distance1 - distance2);
        if (diffDistance > 7)
        {
            return double.PositiveInfinity;
        }

        var angle1 = GetMetric(AngleCache, m1.Id, m3.Id, true);
        var angle2 = GetMetric(AngleCache, m2.Id, m4.Id, true);
        var diffAngle = Math.Abs(angle1 - angle2);
        if (diffAngle > 45)
        {
            return double.PositiveInfinity;
        }

        return diffDistance / 7 + diffAngle / 45;
    }

    private static IEnumerable<MinutiaComparisonResult> CompareGlobal(IEnumerable<Minutia> first, IEnumerable<Minutia> second)
    {
        var copy1 = first.ToList();
        var copy2 = second.ToList();
        if (copy1.Count == 0 || copy2.Count == 0) yield break;

        var used1 = new HashSet<int>(copy1.Count);
        var used2 = new HashSet<int>(copy2.Count);
        var maxMatches = Math.Min(copy1.Count, copy2.Count);

        var pq = new PriorityQueue<MinutiaComparisonResult, double>(copy1.Count * copy2.Count);
        for (int i = 0; i < copy1.Count; i++)
        {
            var m1 = copy1[i];
            for (int j = 0; j < copy2.Count; j++)
            {
                var m2 = copy2[j];
                var deltaX = m1.X - m2.X;
                var deltaY = m1.Y - m2.Y;
                var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                if (distance > 15) continue;

                var featureAngleDiff = (Math.Abs(m1.Theta - m2.Theta) + 2 * Math.PI) % (2 * Math.PI) / Math.PI * 180;
                if (featureAngleDiff > 15) continue;

                var score = distance / 15 + featureAngleDiff / 15 + (m1.IsTermination != m2.IsTermination ? 1 : 0);
                pq.Enqueue(new MinutiaComparisonResult(m1.Id, m2.Id, score), score);
            }
        }

        while (pq.Count > 0 && used1.Count < maxMatches && used2.Count < maxMatches)
        {
            var result = pq.Dequeue();
            if (!used1.Contains(result.MinutiaId1) && !used2.Contains(result.MinutiaId2))
            {
                used1.Add(result.MinutiaId1);
                used2.Add(result.MinutiaId2);
                yield return result;
            }
        }
    }

    private IEnumerable<Minutia> ShiftMinutia(int i2, int m1, int m2)
    {
        var minutia1 = MinutiaeById[m1];
        var minutia2 = MinutiaeById[m2];
        var deltaX = minutia1.X - minutia2.X;
        var deltaY = minutia1.Y - minutia2.Y;

        return MinutiaeByImageId[i2].Select(x => new Minutia
        {
            Id = x.Id,
            X = x.X + deltaX,
            Y = x.Y + deltaY,
            IsTermination = x.IsTermination,
            Theta = x.Theta,
            ImageId = x.ImageId,
            Image = x.Image
        });
    }
}
