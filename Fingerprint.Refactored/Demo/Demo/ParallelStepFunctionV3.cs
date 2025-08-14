using Demo.Entities;
using System.Collections.Concurrent;

namespace Demo;

sealed class ParallelStepFunctionV3(List<Minutia> minutiae) : IStepFunction
{
    public IEnumerable<GlobalComparisonResult> GlobalResults { get; private set; } = [];

    private static int ProcessorCount { get; } = Environment.ProcessorCount;

    private List<Minutia> Minutiae { get; } = [.. minutiae];

    private ConcurrentDictionary<(int Id1, int Id2), double> DistanceCache { get; set; } = new();

    private ConcurrentDictionary<(int Id1, int Id2), double> AngleCache { get; set; } = new();

    private IGrouping<int, Minutia>[] Groups { get; set; } = [];

    private LocalComparisonResult[] LocalResults { get; set; } = [];

    private Dictionary<int, Minutia> MinutiaeById { get; set; } = [];

    private Dictionary<int, Minutia[]> MinutiaeByImageId { get; set; } = [];

    public void AdjustCoords()
    {
        if (Minutiae.Count > 1000)
        {
            Parallel.For(0, Minutiae.Count, new ParallelOptions { MaxDegreeOfParallelism = ProcessorCount }, AdjustMinutia);
        }

        else
        {
            for (int i = 0; i < Minutiae.Count; i++)
            {
                AdjustMinutia(i);
            }
        }

        var squares = GetSquares();
        Groups = [.. GetMinutiaeInSquares(squares)];
        MinutiaeById = Minutiae.ToDictionary(m => m.Id);
        MinutiaeByImageId = Minutiae.GroupBy(x => x.ImageId).ToDictionary(g => g.Key, g => g.ToArray());


        void AdjustMinutia(int index)
        {
            var minutia = Minutiae[index];
            minutia.X -= minutia.Image.WidthShift ?? 0;
            minutia.Y -= minutia.Image.HeightShift ?? 0;
        }
    }

    public void CalculateMetrics()
    {
        Parallel.ForEach(Groups, new ParallelOptions { MaxDegreeOfParallelism = ProcessorCount }, group =>
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

                    // Using ConcurrentDictionary to avoid locks
                    DistanceCache[(leadMinutia.Id, otherMinutia.Id)] = distance;
                    AngleCache[(leadMinutia.Id, otherMinutia.Id)] = angleInDegrees;
                }
            }
        });
    }

    public void MakeGlobalComparison()
    {
        var groups = LocalResults.GroupBy(x => (x.ImageId1, x.ImageId2)).ToArray();
        var results = new ConcurrentBag<GlobalComparisonResult>();

        Parallel.ForEach(groups, new ParallelOptions { MaxDegreeOfParallelism = ProcessorCount }, group =>
        {
            if (group.Key.ImageId1 > group.Key.ImageId2) return;

            var groupElems = group.ToArray();
            var m1 = MinutiaeByImageId[group.Key.ImageId1];
            var m2 = MinutiaeByImageId[group.Key.ImageId2];
            var scores = new ConcurrentBag<(int m1, int m2, double score)>();

            // Process in batches to reduce contention
            Parallel.ForEach(Partitioner.Create(0, groupElems.Length), range =>
            {
                var localScores = new List<(int m1, int m2, double score)>();

                for (int i = range.Item1; i < range.Item2; i++)
                {
                    var localResult = groupElems[i];
                    var shift = ShiftMinutia(group.Key.ImageId2, localResult.TargetMinutiaId1, localResult.TargetMinutiaId2);
                    var globalResult = CompareGlobal(m1, shift);
                    double score = globalResult.Length / (double)Math.Min(m1.Length, m2.Length) * 100;
                    localScores.Add((localResult.TargetMinutiaId1, localResult.TargetMinutiaId2, score));

                    // Limit to top 5 results for further processing to reduce computation
                    var topResults = globalResult.OrderBy(x => x.Score).Take(5).ToArray();
                    foreach (var result in topResults)
                    {
                        var shift1 = ShiftMinutia(group.Key.ImageId2, result.MinutiaId1, result.MinutiaId2);
                        var globalResult1 = CompareGlobal(m1, shift1);
                        double score1 = globalResult1.Length / (double)Math.Min(m1.Length, m2.Length) * 100;
                        localScores.Add((result.MinutiaId1, result.MinutiaId2, score1));
                    }
                }

                // Add batch results to the concurrent bag
                foreach (var score in localScores)
                {
                    scores.Add(score);
                }
            });

            var maxScore = scores.MaxBy(x => x.score);
            results.Add(new GlobalComparisonResult(group.Key.ImageId1, group.Key.ImageId2, maxScore.score, maxScore.m1, maxScore.m2));
            results.Add(new GlobalComparisonResult(group.Key.ImageId2, group.Key.ImageId1, maxScore.score, maxScore.m2, maxScore.m1));
        });

        GlobalResults = results.ToArray();
    }

    public void MakeLocalComparison()
    {
        var groups = Groups.Select(g => g.ToArray()).ToList();
        var results = new ConcurrentBag<LocalComparisonResult>();

        // Pre-generate all group pairs to avoid nested parallelism
        var pairs = Enumerable.Range(0, groups.Count)
            .SelectMany(i => Enumerable.Range(i + 1, groups.Count - i - 1).Select(j => (i, j)))
            .ToArray();

        Parallel.ForEach(pairs, new ParallelOptions { MaxDegreeOfParallelism = ProcessorCount }, pair =>
        {
            var group1 = groups[pair.i];
            var group2 = groups[pair.j];
            var localResults = new List<LocalComparisonResult>();

            // Pre-compute "others" collections to avoid repeated filtering
            var othersCache1 = new Dictionary<int, Minutia[]>(group1.Length);
            var othersCache2 = new Dictionary<int, Minutia[]>(group2.Length);

            foreach (var m in group1)
            {
                othersCache1[m.Id] = group1.Where(x => x.Id != m.Id).ToArray();
            }

            foreach (var m in group2)
            {
                othersCache2[m.Id] = group2.Where(x => x.Id != m.Id).ToArray();
            }

            for (int k = 0; k < group1.Length; k++)
            {
                var minutia1 = group1[k];
                var others1 = othersCache1[minutia1.Id];

                for (int l = 0; l < group2.Length; l++)
                {
                    var minutia2 = group2[l];
                    var others2 = othersCache2[minutia2.Id];

                    var similarity = GetSquareSimilarity(minutia1, minutia2, others1, others2);
                    if (similarity >= 30)
                    {
                        localResults.Add(new LocalComparisonResult(minutia1.ImageId, minutia2.ImageId, minutia1.Id, minutia2.Id, similarity));
                        localResults.Add(new LocalComparisonResult(minutia2.ImageId, minutia1.ImageId, minutia2.Id, minutia1.Id, similarity));
                    }
                }
            }

            foreach (var result in localResults)
            {
                results.Add(result);
            }
        });

        LocalResults = [.. results];
    }

    private Dictionary<int, (double X, double Y)> GetSquares()
    {
        // Pre-size the dictionary to avoid resizing
        var sums = new Dictionary<int, (double sumX, double sumY, int count)>(80);
        foreach (var m in Minutiae)
        {
            if (!sums.TryGetValue(m.ImageId, out var data))
            {
                data = (0, 0, 0);
            }
            sums[m.ImageId] = (data.sumX + m.X, data.sumY + m.Y, data.count + 1);
        }

        var result = new Dictionary<int, (double X, double Y)>(sums.Count);
        foreach (var kvp in sums)
        {
            result[kvp.Key] = (kvp.Value.sumX / kvp.Value.count, kvp.Value.sumY / kvp.Value.count);
        }
        return result;
    }

    private IEnumerable<IGrouping<int, Minutia>> GetMinutiaeInSquares(Dictionary<int, (double X, double Y)> squares)
    {
        const int halfSize = 75;
        return [.. Minutiae
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
            .Where(g => g.Any())]; // Force materialization
    }

    private double GetMetric(IDictionary<(int Id1, int Id2), double> metrics, int Id1, int Id2, bool isAngle = false)
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

    private double GetSquareSimilarity(Minutia m1, Minutia m2, Minutia[] ml1, Minutia[] ml2)
    {
        if (ml1.Length == 0 || ml2.Length == 0) return 0;

        var used1 = new HashSet<int>(ml1.Length);
        var used2 = new HashSet<int>(ml2.Length);
        var score = 0;
        var maxMatches = Math.Min(ml1.Length, ml2.Length);

        // Use array instead of List for better performance
        var convolutions = new (int id1, int id2, double equality)[ml1.Length * ml2.Length];
        int convCount = 0;

        for (int i = 0; i < ml1.Length; i++)
        {
            for (int j = 0; j < ml2.Length; j++)
            {
                double conv = CalculateLocalConvolution(m1, m2, ml1[i], ml2[j]);
                if (conv != double.PositiveInfinity)
                {
                    convolutions[convCount++] = (ml1[i].Id, ml2[j].Id, conv);
                }
            }
        }

        // Sort only the valid portion of the array
        Array.Sort(convolutions, 0, convCount, Comparer<(int, int, double)>.Create((a, b) => a.Item3.CompareTo(b.Item3)));

        for (int i = 0; i < convCount; i++)
        {
            if (score >= maxMatches) break;
            var (id1, id2, equality) = convolutions[i];
            if (!used1.Contains(id1) && !used2.Contains(id2))
            {
                used1.Add(id1);
                used2.Add(id2);
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

    private static MinutiaComparisonResult[] CompareGlobal(Minutia[] first, Minutia[] second)
    {
        if (first.Length == 0 || second.Length == 0) return [];

        var used1 = new HashSet<int>(first.Length);
        var used2 = new HashSet<int>(second.Length);
        var maxMatches = Math.Min(first.Length, second.Length);

        // Pre-allocate array with approximate capacity to avoid resizing
        var results = new List<MinutiaComparisonResult>(maxMatches);

        // Use array instead of priority queue for better performance
        var matches = new List<(MinutiaComparisonResult result, double score)>(first.Length * second.Length);

        for (int i = 0; i < first.Length; i++)
        {
            var m1 = first[i];
            for (int j = 0; j < second.Length; j++)
            {
                var m2 = second[j];
                var deltaX = m1.X - m2.X;
                var deltaY = m1.Y - m2.Y;
                var distanceSquared = deltaX * deltaX + deltaY * deltaY;

                // Avoid square root when possible by comparing squared values
                if (distanceSquared > 225) continue; // 15^2 = 225

                var distance = Math.Sqrt(distanceSquared);
                var featureAngleDiff = (Math.Abs(m1.Theta - m2.Theta) + 2 * Math.PI) % (2 * Math.PI) / Math.PI * 180;
                if (featureAngleDiff > 15) continue;

                var score = distance / 15 + featureAngleDiff / 15 + (m1.IsTermination != m2.IsTermination ? 1 : 0);
                var result = new MinutiaComparisonResult(m1.Id, m2.Id, score);
                matches.Add((result, score));
            }
        }

        // Sort by score
        matches.Sort((a, b) => a.score.CompareTo(b.score));

        foreach (var match in matches)
        {
            if (used1.Count >= maxMatches || used2.Count >= maxMatches) break;

            var result = match.result;
            if (!used1.Contains(result.MinutiaId1) && !used2.Contains(result.MinutiaId2))
            {
                used1.Add(result.MinutiaId1);
                used2.Add(result.MinutiaId2);
                results.Add(result);
            }
        }

        return [.. results];
    }

    private Minutia[] ShiftMinutia(int i2, int m1, int m2)
    {
        var minutia1 = MinutiaeById[m1];
        var minutia2 = MinutiaeById[m2];
        var deltaX = minutia1.X - minutia2.X;
        var deltaY = minutia1.Y - minutia2.Y;

        var baseMinutiae = MinutiaeByImageId[i2];
        var shifted = new Minutia[baseMinutiae.Length];

        // Shift in a single pass to reduce allocations
        for (int i = 0; i < baseMinutiae.Length; i++)
        {
            var x = baseMinutiae[i];
            shifted[i] = new Minutia
            {
                Id = x.Id,
                X = x.X + deltaX,
                Y = x.Y + deltaY,
                IsTermination = x.IsTermination,
                Theta = x.Theta,
                ImageId = x.ImageId,
                Image = x.Image
            };
        }

        return shifted;
    }
}
