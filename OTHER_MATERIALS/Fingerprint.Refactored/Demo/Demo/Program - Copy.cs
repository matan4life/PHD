//// See https://aka.ms/new-console-template for more information
//using Demo;
//using Demo.Database;
//using Demo.Entities;
//using Microsoft.EntityFrameworkCore;
//using System.Collections.Concurrent;
//using System.Diagnostics;
//using System.Text;

//var context = new FingerprintContext();

//var images = await context.Images
//    .Where(x => x.TestRunId == 1)
//    .Include(x => x.Minutiae)
//    .AsNoTracking()
//    .ToListAsync();

//var minutiae = images.SelectMany(x => x.Minutiae).ToList();

//var distanceCache = new Dictionary<(int Id1, int Id2), double>();
//var angleCache = new Dictionary<(int Id1, int Id2), double>();

//void AdjustCoords(List<Minutia> minutiae)
//    => minutiae.ForEach(m =>
//    {
//        m.X -= m.Image.WidthShift ?? 0;
//        m.Y -= m.Image.HeightShift ?? 0;
//    });

//void AdjustCoords1(List<Minutia> minutiae)
//{
//    for (int i = 0; i < minutiae.Count; i++)
//    {
//        Minutia m = minutiae[i];
//        m.X -= m.Image.WidthShift ?? 0;
//        m.Y -= m.Image.HeightShift ?? 0;
//    }
//}

//IDictionary<int, (double X, double Y)> GetSquares(List<Minutia> minutiae)
//    => minutiae.GroupBy(x => x.ImageId)
//        .Select(x => new KeyValuePair<int, (double X, double Y)>(x.Key, (x.Average(x1 => x1.X), x.Average(x1 => x1.Y))))
//        .ToDictionary(x => x.Key, x => x.Value);

//IDictionary<int, (double X, double Y)> GetSquares1(List<Minutia> minutiae)
//{
//    var sums = new Dictionary<int, (double sumX, double sumY, int count)>(80);
//    foreach (var m in minutiae)
//    {
//        if (!sums.TryGetValue(m.ImageId, out var data))
//        {
//            data = (0, 0, 0);
//        }
//        sums[m.ImageId] = (data.sumX + m.X, data.sumY + m.Y, data.count + 1);
//    }

//    var result = new Dictionary<int, (double X, double Y)>(80);
//    foreach (var kvp in sums)
//    {
//        result[kvp.Key] = (kvp.Value.sumX / kvp.Value.count, kvp.Value.sumY / kvp.Value.count);
//    }
//    return result;
//}

//bool IsInSquare(Minutia minutia, IDictionary<int, (double X, double Y)> squares)
//{
//    const int squareSizeHalfed = 75;
//    var (X, Y) = squares[minutia.ImageId];
//    return minutia.X >= X - squareSizeHalfed && minutia.X <= X + squareSizeHalfed && minutia.Y >= Y - squareSizeHalfed && minutia.Y <= Y + squareSizeHalfed;
//}

//IEnumerable<IGrouping<int, Minutia>> GetMinutiaeInSquares(List<Minutia> minutiae, IDictionary<int, (double X, double Y)> squares)
//{
//    const int halfSize = 75;
//    return minutiae
//        .GroupBy(x => x.ImageId)
//        .Select(g =>
//        {
//            var (centerX, centerY) = squares[g.Key];
//            double minX = centerX - halfSize;
//            double maxX = centerX + halfSize;
//            double minY = centerY - halfSize;
//            double maxY = centerY + halfSize;
//            return g.Where(m => m.X >= minX && m.X <= maxX && m.Y >= minY && m.Y <= maxY)
//                    .AsGrouping(g.Key);
//        })
//        .Where(g => g.Any());
//}

//void CalculateMetrics(List<Minutia> minutiae, IDictionary<int, (double X, double Y)> squares)
//{
//    foreach (var group in minutiae.Where(x => IsInSquare(x, squares)).GroupBy(x => x.ImageId))
//    {
//        var filteredMinutiae = group.ToList();
//        int length = filteredMinutiae.Count;

//        for (int i = 0; i < length; i++)
//        {
//            var leadMinutia = filteredMinutiae[i];
//            for (int j = i + 1; j < length; j++)
//            {
//                var otherMinutia = filteredMinutiae[j];

//                var deltaX = leadMinutia.X - otherMinutia.X;
//                var deltaY = leadMinutia.Y - otherMinutia.Y;

//                var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
//                var angleInRads = Math.Atan2(deltaY, deltaX);
//                var angleInDegrees = ((angleInRads + 2 * Math.PI) % (2 * Math.PI)) / Math.PI * 180;

//                distanceCache[(leadMinutia.Id, otherMinutia.Id)] = distance;
//                angleCache[(leadMinutia.Id, otherMinutia.Id)] = angleInDegrees;
//            }
//        }
//    }
//}

//void CalculateMetrics1(IEnumerable<IGrouping<int, Minutia>> groups)
//{
//    foreach (var group in groups)
//    {
//        var filteredMinutiae = group.ToArray();
//        int length = filteredMinutiae.Length;

//        for (int i = 0; i < length; i++)
//        {
//            var leadMinutia = filteredMinutiae[i];
//            for (int j = i + 1; j < length; j++)
//            {
//                var otherMinutia = filteredMinutiae[j];

//                var deltaX = leadMinutia.X - otherMinutia.X;
//                var deltaY = leadMinutia.Y - otherMinutia.Y;

//                var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
//                var angleInRads = Math.Atan2(deltaY, deltaX);
//                var angleInDegrees = ((angleInRads + 2 * Math.PI) % (2 * Math.PI)) / Math.PI * 180;

//                distanceCache[(leadMinutia.Id, otherMinutia.Id)] = distance;
//                angleCache[(leadMinutia.Id, otherMinutia.Id)] = angleInDegrees;
//            }
//        }
//    }
//}

//double GetMetric(IDictionary<(int Id1, int Id2), double> metrics, int Id1, int Id2, Func<double, double>? transform = null)
//{
//    if (metrics.TryGetValue((Id1, Id2), out var metric))
//    {
//        return metric;
//    }
//    if (metrics.TryGetValue((Id2, Id1), out metric))
//    {
//        return transform?.Invoke(metric) ?? metric;
//    }
//    throw new InvalidOperationException("Metric not found");
//}

//double GetMetric1(IDictionary<(int Id1, int Id2), double> metrics, int Id1, int Id2, bool isAngle = false)
//{
//    if (metrics.TryGetValue((Id1, Id2), out var metric))
//    {
//        return metric;
//    }

//    if (metrics.TryGetValue((Id2, Id1), out var reversedMetric))
//    {
//        return isAngle ? 360 - reversedMetric : reversedMetric;
//    }

//    return double.NaN;
//}

//IEnumerable<LocalComparisonResult> CompareLocally(List<Minutia> minutiae, IDictionary<int, (double X, double Y)> squares)
//{
//    var groups = minutiae.Where(x => IsInSquare(x, squares)).GroupBy(x => x.ImageId).ToList();
//    var maxGroupSize = groups.Max(x => x.Count());
//    var secondPos = GetPositionSize(maxGroupSize);
//    var thirdPos = GetPositionSize(maxGroupSize * secondPos);
//    var fourthPos = GetPositionSize(groups.Count * thirdPos);
//    var result = new LocalComparisonResult[groups.Count * fourthPos + groups.Count * thirdPos + maxGroupSize * secondPos + maxGroupSize];
//    Parallel.For(0, groups.Count, i =>
//    {
//        var group1 = groups[i].ToList();
//        Parallel.For(i + 1, groups.Count, j =>
//        {
//            var group2 = groups[j].ToList();
//            Parallel.For(0, group1.Count, k =>
//            {
//                var minutia1 = group1[k];
//                Parallel.For(0, group2.Count, l =>
//                {
//                    var minutia2 = group2[l];
//                    var similarity = GetSquareSimilarity(minutia1, minutia2, group1.Where(x => x.Id != minutia1.Id), group2.Where(x => x.Id != minutia2.Id));
//                    if (similarity >= 30)
//                    {
//                        int index1 = i * fourthPos + j * thirdPos + k * secondPos + l;
//                        int index2 = j * fourthPos + i * thirdPos + l * secondPos + k;
//                        result[index1] = new LocalComparisonResult(minutia1.ImageId, minutia2.ImageId, minutia1.Id, minutia2.Id, similarity);
//                        result[index2] = new LocalComparisonResult(minutia2.ImageId, minutia1.ImageId, minutia2.Id, minutia1.Id, similarity);
//                    }
//                });
//            });
//        });
//    });

//    return [.. result.Where(x => x is not null)];
//    //for (int i = 0; i < groups.Count; i++)
//    //{
//    //    var group1 = groups[i].ToList();
//    //    for (int j = i + 1; j < groups.Count; j++)
//    //    {
//    //        var group2 = groups[j].ToList();
//    //        for (int k = 0; k < group1.Count; k++)
//    //        {
//    //            var minutia1 = group1[k];
//    //            for (int l = 0; l < group2.Count; l++)
//    //            {
//    //                var minutia2 = group2[l];
//    //                var similarity = GetSquareSimilarity(minutia1, minutia2, group1.Where(x => x.Id != minutia1.Id), group2.Where(x => x.Id != minutia2.Id));
//    //                if (similarity >= 30)
//    //                {
//    //                    yield return new LocalComparisonResult(minutia1.ImageId, minutia2.ImageId, minutia1.Id, minutia2.Id, similarity);
//    //                    yield return new LocalComparisonResult(minutia2.ImageId, minutia1.ImageId, minutia2.Id, minutia1.Id, similarity);
//    //                }
//    //            }
//    //        }
//    //        //Console.WriteLine($"Processed local comparisons for images {groups[i].Key} and {groups[j].Key}");
//    //    }
//    //}
//}

//IEnumerable<LocalComparisonResult> CompareLocally1(IEnumerable<IGrouping<int, Minutia>> groups1)
//{
//    // Pre-filter and group efficiently
//    var groups = groups1.Select(g => g.ToList()).ToList();

//    // Thread-local results to reduce contention
//    var results = new ConcurrentBag<LocalComparisonResult>();

//    // Two-level parallelism: outer groups, inner pairs
//    Parallel.For(0, groups.Count, i =>
//    {
//        var group1 = groups[i];
//        var localResults = new List<LocalComparisonResult>(); // Thread-local buffer
//        for (int j = i + 1; j < groups.Count; j++)
//        {
//            var group2 = groups[j];
//            Parallel.For(0, group1.Count, k =>
//            {
//                var minutia1 = group1[k];
//                var others1 = group1.Where(x => x.Id != minutia1.Id);
//                for (int l = 0; l < group2.Count; l++)
//                {
//                    var minutia2 = group2[l];
//                    var others2 = group2.Where(x => x.Id != minutia2.Id);
//                    var similarity = GetSquareSimilarity1(minutia1, minutia2, others1, others2);
//                    if (similarity >= 30)
//                    {
//                        lock (localResults) // Minimal contention within thread
//                        {
//                            localResults.Add(new LocalComparisonResult(minutia1.ImageId, minutia2.ImageId, minutia1.Id, minutia2.Id, similarity));
//                            localResults.Add(new LocalComparisonResult(minutia2.ImageId, minutia1.ImageId, minutia2.Id, minutia1.Id, similarity));
//                        }
//                    }
//                }
//            });
//        }
//        // Batch add to global results
//        foreach (var result in localResults) results.Add(result);
//    });

//    return results;
//}

//int GetPositionSize(int previousMaxValue) => (int)Math.Pow(10, (int)Math.Log10(previousMaxValue) + 1);

//double GetSquareSimilarity(Minutia m1, Minutia m2, IEnumerable<Minutia> ml1, IEnumerable<Minutia> ml2)
//{
//    var ml1Copy = ml1.ToList();
//    var ml2Copy = ml2.ToList();
//    var score = 0;
//    var convolution = from firstMinutia in ml1
//                      from secondMinutia in ml2
//                      select new { FirstMinutiaId = firstMinutia.Id, SecondMinutiaId = secondMinutia.Id, Equality = CalculateLocalConvolution(m1, m2, firstMinutia, secondMinutia) }
//                      into convolutionResult
//                      where convolutionResult.Equality != double.PositiveInfinity
//                      select convolutionResult
//                      into convolutionResultOrdered
//                      orderby convolutionResultOrdered.Equality ascending
//                      select convolutionResultOrdered;
//    var result = convolution.ToList();
//    while (ml1Copy.Count > 0 && ml2Copy.Count > 0 && result.Count > 0)
//    {
//        var convolutionResult = result.First();
//        ml1Copy.RemoveAll(x => x.Id == convolutionResult.FirstMinutiaId);
//        ml2Copy.RemoveAll(x => x.Id == convolutionResult.SecondMinutiaId);
//        result.RemoveAll(x => x.FirstMinutiaId == convolutionResult.FirstMinutiaId || x.SecondMinutiaId == convolutionResult.SecondMinutiaId);
//        score++;
//    }

//    return (double)score / Math.Min(ml1.Count(), ml2.Count()) * 100;
//}

//double GetSquareSimilarity1(Minutia m1, Minutia m2, IEnumerable<Minutia> ml1, IEnumerable<Minutia> ml2)
//{
//    var ml1List = ml1.ToList();
//    var ml2List = ml2.ToList();
//    if (ml1List.Count == 0 || ml2List.Count == 0) return 0;

//    var used1 = new HashSet<int>();
//    var used2 = new HashSet<int>();
//    var score = 0;
//    var maxMatches = Math.Min(ml1List.Count, ml2List.Count);

//    var pq = new PriorityQueue<(int id1, int id2, double equality), double>(ml1List.Count * ml2List.Count);
//    for (int i = 0; i < ml1List.Count; i++)
//    {
//        for (int j = 0; j < ml2List.Count; j++)
//        {
//            double conv = CalculateLocalConvolution1(m1, m2, ml1List[i], ml2List[j]);
//            if (conv != double.PositiveInfinity)
//            {
//                pq.Enqueue((ml1List[i].Id, ml2List[j].Id, conv), conv);
//            }
//        }
//    }

//    while (score < maxMatches && pq.TryDequeue(out var match, out _))
//    {
//        if (!used1.Contains(match.id1) && !used2.Contains(match.id2))
//        {
//            used1.Add(match.id1);
//            used2.Add(match.id2);
//            score++;
//        }
//    }

//    return (double)score / maxMatches * 100;
//}

//double CalculateLocalConvolution(Minutia m1, Minutia m2, Minutia m3, Minutia m4)
//{
//    var distance1 = GetMetric(distanceCache, m1.Id, m3.Id);
//    var distance2 = GetMetric(distanceCache, m2.Id, m4.Id);
//    if (Math.Abs(distance1 - distance2) > 7)
//    {
//        return double.PositiveInfinity;
//    }

//    var angle1 = GetMetric(angleCache, m1.Id, m3.Id, angle => 360 - angle);
//    var angle2 = GetMetric(angleCache, m2.Id, m4.Id, angle => 360 - angle);
//    if (Math.Abs(angle1 - angle2) > 45)
//    {
//        return double.PositiveInfinity;
//    }

//    return Math.Abs(distance1 - distance2) / 7 + Math.Abs(angle1 - angle2) / 45;
//}

//double CalculateLocalConvolution1(Minutia m1, Minutia m2, Minutia m3, Minutia m4)
//{
//    var distance1 = GetMetric1(distanceCache, m1.Id, m3.Id);
//    var distance2 = GetMetric1(distanceCache, m2.Id, m4.Id);
//    var diffDistance = Math.Abs(distance1 - distance2);
//    if (diffDistance > 7)
//    {
//        return double.PositiveInfinity;
//    }

//    var angle1 = GetMetric1(angleCache, m1.Id, m3.Id, true);
//    var angle2 = GetMetric1(angleCache, m2.Id, m4.Id, true);
//    var diffAngle = Math.Abs(angle1 - angle2);
//    if (diffAngle > 45)
//    {
//        return double.PositiveInfinity;
//    }

//    return diffDistance / 7 + diffAngle / 45;
//}

//IEnumerable<GlobalComparisonResult> ApplyGlobalComparison(IEnumerable<LocalComparisonResult> localResults, IDictionary<int, (double X, double Y)> squares)
//{
//    var groups = localResults.GroupBy(x => (x.ImageId1, x.ImageId2)).ToList();
//    var results = new List<GlobalComparisonResult>();
//    Parallel.For(0, groups.Count, (k, state) =>
//    {
//        var group = groups[k];
//        if (group.Key.ImageId1 > group.Key.ImageId2)
//        {
//            return;
//        }

//        var groupElems = group.ToList();
//        var m1 = minutiae.Where(x => x.ImageId == group.Key.ImageId1).ToList();
//        var m2 = minutiae.Where(x => x.ImageId == group.Key.ImageId2).ToList();
//        ConcurrentBag<(int m1, int m2, double score)> scores = [];
//        Parallel.For(0, groupElems.Count, i =>
//        {
//            var localResult = groupElems[i];
//            var shift = ShiftMinutia(group.Key.ImageId2, localResult.TargetMinutiaId1, localResult.TargetMinutiaId2).ToList();
//            var globalResult = CompareGlobal(m1, shift).ToList();
//            scores.Add((localResult.TargetMinutiaId1, localResult.TargetMinutiaId2, globalResult.Count / (double)Math.Min(m1.Count, m2.Count) * 100));
//            Parallel.For(0, globalResult.Count, j =>
//            {
//                var result = globalResult[j];
//                var shift1 = ShiftMinutia(group.Key.ImageId2, result.MinutiaId1, result.MinutiaId2).ToList();
//                var globalResult1 = CompareGlobal(m1, shift1).ToList();
//                scores.Add((result.MinutiaId1, result.MinutiaId2, globalResult1.Count / (double)Math.Min(m1.Count, m2.Count) * 100));
//            });
//        });

//        var max = scores.MaxBy(x => x.score);
//        results.Add(new GlobalComparisonResult(group.Key.ImageId1, group.Key.ImageId2, max.score, max.m1, max.m2));
//        results.Add(new GlobalComparisonResult(group.Key.ImageId2, group.Key.ImageId1, max.score, max.m2, max.m1));
//    });

//    return [.. results];
//}


//Dictionary<int, Minutia> minutiaeById = [];
//Dictionary<int, List<Minutia>> minutiaeByImageId = [];

//IEnumerable<GlobalComparisonResult> ApplyGlobalComparison1(IEnumerable<LocalComparisonResult> localResults)
//{
//    var groups = localResults.GroupBy(x => (x.ImageId1, x.ImageId2)).ToList();
//    var results = new ConcurrentBag<GlobalComparisonResult>();

//    Parallel.For(0, groups.Count, k =>
//    {
//        var group = groups[k];
//        if (group.Key.ImageId1 > group.Key.ImageId2) return;

//        var groupElems = group.ToList();
//        var m1 = minutiaeByImageId[group.Key.ImageId1];
//        var m2 = minutiaeByImageId[group.Key.ImageId2];
//        var scores = new List<(int m1, int m2, double score)>(groupElems.Count * 62); // Pre-size for ~62 inner iterations

//        Parallel.For(0, groupElems.Count, i =>
//        {
//            var localResult = groupElems[i];
//            var shift = ShiftMinutia1(group.Key.ImageId2, localResult.TargetMinutiaId1, localResult.TargetMinutiaId2).ToList();
//            var globalResult = CompareGlobal1(m1, shift).ToList();
//            double score = globalResult.Count / (double)Math.Min(m1.Count, m2.Count) * 100;
//            lock (scores)
//            {
//                scores.Add((localResult.TargetMinutiaId1, localResult.TargetMinutiaId2, score));
//            }

//            Parallel.For(0, globalResult.Count, j =>
//            {
//                var result = globalResult[j];
//                var shift1 = ShiftMinutia1(group.Key.ImageId2, result.MinutiaId1, result.MinutiaId2).ToList();
//                var test = localResult;
//                var globalResult1 = CompareGlobal1(m1, shift1).ToList();
//                double score1 = globalResult1.Count / (double)Math.Min(m1.Count, m2.Count) * 100;
//                lock (scores)
//                {
//                    scores.Add((result.MinutiaId1, result.MinutiaId2, score1));
//                }
//            });
//        });

//        var maxScore = scores.MaxBy(x => x.score);
//        results.Add(new GlobalComparisonResult(group.Key.ImageId1, group.Key.ImageId2, maxScore.score, maxScore.m1, maxScore.m2));
//        results.Add(new GlobalComparisonResult(group.Key.ImageId2, group.Key.ImageId1, maxScore.score, maxScore.m2, maxScore.m1));
//    });

//    return results;
//}

//IEnumerable<Minutia> ShiftMinutia1(int i2, int m1, int m2)
//{
//    var minutia1 = minutiaeById[m1];
//    var minutia2 = minutiaeById[m2];
//    var deltaX = minutia1.X - minutia2.X;
//    var deltaY = minutia1.Y - minutia2.Y;

//    return minutiaeByImageId[i2].Select(x => new Minutia
//    {
//        Id = x.Id,
//        X = x.X + deltaX,
//        Y = x.Y + deltaY,
//        IsTermination = x.IsTermination,
//        Theta = x.Theta,
//        ImageId = x.ImageId,
//        Image = x.Image
//    });
//}

//IEnumerable<MinutiaComparisonResult> CompareGlobal1(IEnumerable<Minutia> first, IEnumerable<Minutia> second)
//{
//    var copy1 = first.ToList();
//    var copy2 = second.ToList();
//    if (copy1.Count == 0 || copy2.Count == 0) yield break;

//    var used1 = new HashSet<int>();
//    var used2 = new HashSet<int>();
//    var maxMatches = Math.Min(copy1.Count, copy2.Count);

//    var pq = new PriorityQueue<MinutiaComparisonResult, double>(copy1.Count * copy2.Count);
//    for (int i = 0; i < copy1.Count; i++)
//    {
//        var m1 = copy1[i];
//        for (int j = 0; j < copy2.Count; j++)
//        {
//            var m2 = copy2[j];
//            var deltaX = m1.X - m2.X;
//            var deltaY = m1.Y - m2.Y;
//            var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
//            if (distance > 15) continue;

//            var featureAngleDiff = (Math.Abs(m1.Theta - m2.Theta) + 2 * Math.PI) % (2 * Math.PI) / Math.PI * 180;
//            if (featureAngleDiff > 15) continue;

//            var score = distance / 15 + featureAngleDiff / 15 + (m1.IsTermination != m2.IsTermination ? 1 : 0);
//            pq.Enqueue(new MinutiaComparisonResult(m1.Id, m2.Id, score), score);
//        }
//    }

//    while (pq.Count > 0 && used1.Count < maxMatches && used2.Count < maxMatches)
//    {
//        var result = pq.Dequeue();
//        if (!used1.Contains(result.MinutiaId1) && !used2.Contains(result.MinutiaId2))
//        {
//            used1.Add(result.MinutiaId1);
//            used2.Add(result.MinutiaId2);
//            yield return result;
//        }
//    }
//}

//double CalculateGlobalConvolution(Minutia m1, Minutia m2)
//{
//    var deltaX = m1.X - m2.X;
//    var deltaY = m1.Y - m2.Y;
//    var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
//    if (distance > 15)
//    {
//        return double.PositiveInfinity;
//    }

//    var featureAngleDiff = (Math.Abs(m1.Theta - m2.Theta) + 2 * Math.PI) % (2 * Math.PI) / Math.PI * 180;
//    if (featureAngleDiff > 15)
//    {
//        return double.PositiveInfinity;
//    }

//    return distance / 15 + featureAngleDiff / 15 + m1.IsTermination != m2.IsTermination ? 1 : 0;
//}

//IEnumerable<MinutiaComparisonResult> CompareGlobal(IEnumerable<Minutia> first, IEnumerable<Minutia> second)
//{
//    //var copy1 = first.Where(x => x.X <= xMax && x.X >= xMin && x.Y <= yMax && x.Y >= yMin).ToList();
//    //var copy2 = second.Where(x => x.X <= xMax && x.X >= xMin && x.Y <= yMax && x.Y >= yMin).ToList();
//    var copy1 = first.ToList();
//    var copy2 = second.ToList();
//    var result = from firstMinutia in copy1
//                 from secondMinutia in copy2
//                 select new MinutiaComparisonResult(firstMinutia.Id, secondMinutia.Id, CalculateGlobalConvolution(firstMinutia, secondMinutia))
//                 into minutiaComparisonResult
//                 where minutiaComparisonResult.Score != double.PositiveInfinity
//                 select minutiaComparisonResult
//                 into minutiaComparisonResultOrdered
//                 orderby minutiaComparisonResultOrdered.Score ascending
//                 select minutiaComparisonResultOrdered;
//    var copy = result.ToList();

//    while (copy1.Count > 0 && copy2.Count > 0 && copy.Count > 0)
//    {
//        var minutiaComparisonResult = copy.First();
//        copy1.RemoveAll(x => x.Id == minutiaComparisonResult.MinutiaId1);
//        copy2.RemoveAll(x => x.Id == minutiaComparisonResult.MinutiaId2);
//        copy.RemoveAll(x => x.MinutiaId1 == minutiaComparisonResult.MinutiaId1 || x.MinutiaId2 == minutiaComparisonResult.MinutiaId2);
//        yield return minutiaComparisonResult;
//    }
//}

//IEnumerable<Minutia> ShiftMinutia(int i2, int m1, int m2)
//{
//    var minutia1 = minutiae.First(x => x.Id == m1);
//    var minutia2 = minutiae.First(x => x.Id == m2);
//    var deltaX = minutia1.X - minutia2.X;
//    var deltaY = minutia1.Y - minutia2.Y;

//    return minutiae.Where(x => x.ImageId == i2).Select(x => new Minutia
//    {
//        Id = x.Id,
//        X = x.X + deltaX,
//        Y = x.Y + deltaY,
//        IsTermination = x.IsTermination,
//        Theta = x.Theta,
//        ImageId = x.ImageId,
//        Image = x.Image
//    });
//}

//var sw = Stopwatch.StartNew();
////AdjustCoords1(minutiae);
////var squares = GetSquares1(minutiae);
////var groups = GetMinutiaeInSquares(minutiae, squares).ToList();
////CalculateMetrics1(groups);
////var results = CompareLocally1(groups).ToList();
////minutiaeById = minutiae.ToDictionary(m => m.Id);
////minutiaeByImageId = minutiae.GroupBy(x => x.ImageId).ToDictionary(g => g.Key, g => g.ToList());
////var result = ApplyGlobalComparison1(results);
//AdjustCoords(minutiae);
//var squares = GetSquares(minutiae);
//CalculateMetrics(minutiae, squares);
//var results = CompareLocally(minutiae, squares).ToList();
//var result = ApplyGlobalComparison(results, squares);
//sw.Stop();
//Console.WriteLine(sw.Elapsed);

//var ids = result.Select(x => x.ImageId1).Distinct().Order();
//var sb = new StringBuilder()
//    .AppendLine($",{string.Join(",", ids)}");
//var minIndex = images.Min(x => x.Id);
//var maxIndex = images.Max(x => x.Id);
//foreach (var id in ids)
//{
//    sb.Append($"{id},");
//    var idResults = result.Where(x => x.ImageId1 == id).Append(new(id, id, 100, 0, 0));
//    var adjustedResults = Enumerable.Range(minIndex, maxIndex - minIndex + 1).Select(i => idResults.FirstOrDefault(x => x.ImageId2 == i) ?? new GlobalComparisonResult(id, i, 0, 0, 0));
//    sb.AppendLine(string.Join(",", adjustedResults.OrderBy(x => x.ImageId2).Select(x => $"{x.Score:F2}%")));
//}

//await File.WriteAllTextAsync("results1_new.csv", sb.ToString());

//record LocalComparisonResult(int ImageId1, int ImageId2, int TargetMinutiaId1, int TargetMinutiaId2, double Score);

//record GlobalComparisonResult(int ImageId1, int ImageId2, double Score, int MinutiaId1, int MinutiaId2);

//record MinutiaComparisonResult(int MinutiaId1, int MinutiaId2, double Score);
