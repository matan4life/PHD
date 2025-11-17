using Demo.Entities;
using System.Collections.Concurrent;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;

namespace Demo;

/// <summary>
/// Повна SIMD-оптимізація всіх етапів алгоритму порівняння відбитків
/// </summary>
sealed class SimdParallelStepFunction : IStepFunction
{
    private const int VECTOR_SIZE_AVX512 = 8;
    private const int VECTOR_SIZE_AVX2 = 4;
    private const double DISTANCE_THRESHOLD = 7.0;
    private const double ANGLE_THRESHOLD = 45.0;
    private const double GLOBAL_DISTANCE_DELTA = 15.0;
    private const double GLOBAL_ANGLE_DELTA = 12.0;

    public IEnumerable<GlobalComparisonResult> GlobalResults { get; private set; } = [];

    private List<Minutia> Minutiae { get; } = [];
    private Dictionary<(int Id1, int Id2), double> DistanceCache { get; } = [];
    private Dictionary<(int Id1, int Id2), double> AngleCache { get; } = [];
    private IEnumerable<IGrouping<int, Minutia>> Groups { get; set; } = [];
    private IEnumerable<LocalComparisonResult> LocalResults { get; set; } = [];
    private Dictionary<int, Minutia> MinutiaeById { get; set; } = [];
    private Dictionary<int, List<Minutia>> MinutiaeByImageId { get; set; } = [];

    public SimdParallelStepFunction(List<Minutia> minutiae)
    {
        Minutiae.AddRange(minutiae);
    }

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
        MinutiaeByImageId = Minutiae.GroupBy(x => x.ImageId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public void CalculateMetrics()
    {
        if (Avx512F.IsSupported)
            CalculateMetricsAvx512();
        else if (Avx2.IsSupported)
            CalculateMetricsAvx2();
        else
            CalculateMetricsScalar();
    }

    private unsafe void CalculateMetricsAvx512()
    {
        foreach (var group in Groups)
        {
            var filteredMinutiae = group.ToArray();
            int length = filteredMinutiae.Length;

            for (int i = 0; i < length; i++)
            {
                var leadMinutia = filteredMinutiae[i];
                int j = i + 1;

                for (; j + 7 < length; j += 8)
                {
                    Vector512<double> x2 = Vector512.Create(
                        (double)filteredMinutiae[j].X, (double)filteredMinutiae[j + 1].X,
                        (double)filteredMinutiae[j + 2].X, (double)filteredMinutiae[j + 3].X,
                        (double)filteredMinutiae[j + 4].X, (double)filteredMinutiae[j + 5].X,
                        (double)filteredMinutiae[j + 6].X, (double)filteredMinutiae[j + 7].X);

                    Vector512<double> y2 = Vector512.Create(
                        (double)filteredMinutiae[j].Y, (double)filteredMinutiae[j + 1].Y,
                        (double)filteredMinutiae[j + 2].Y, (double)filteredMinutiae[j + 3].Y,
                        (double)filteredMinutiae[j + 4].Y, (double)filteredMinutiae[j + 5].Y,
                        (double)filteredMinutiae[j + 6].Y, (double)filteredMinutiae[j + 7].Y);

                    Vector512<double> x1Vec = Vector512.Create((double)leadMinutia.X);
                    Vector512<double> y1Vec = Vector512.Create((double)leadMinutia.Y);

                    Vector512<double> deltaX = Avx512F.Subtract(x1Vec, x2);
                    Vector512<double> deltaY = Avx512F.Subtract(y1Vec, y2);
                    Vector512<double> deltaX2 = Avx512F.Multiply(deltaX, deltaX);
                    Vector512<double> deltaY2 = Avx512F.Multiply(deltaY, deltaY);
                    Vector512<double> sum = Avx512F.Add(deltaX2, deltaY2);
                    Vector512<double> distances = Avx512F.Sqrt(sum);

                    Span<double> distArray = stackalloc double[8];
                    Span<double> dxArray = stackalloc double[8];
                    Span<double> dyArray = stackalloc double[8];

                    distances.CopyTo(distArray);
                    deltaX.CopyTo(dxArray);
                    deltaY.CopyTo(dyArray);

                    for (int k = 0; k < 8; k++)
                    {
                        var otherId = filteredMinutiae[j + k].Id;
                        DistanceCache[(leadMinutia.Id, otherId)] = distArray[k];

                        var angleInRads = Math.Atan2(dyArray[k], dxArray[k]);
                        var angleInDegrees = ((angleInRads + 2 * Math.PI) % (2 * Math.PI)) / Math.PI * 180;
                        AngleCache[(leadMinutia.Id, otherId)] = angleInDegrees;
                    }
                }

                for (; j < length; j++)
                {
                    var otherMinutia = filteredMinutiae[j];
                    var deltaX = leadMinutia.X - otherMinutia.X;
                    var deltaY = leadMinutia.Y - otherMinutia.Y;
                    var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                    var angleInRads = Math.Atan2(deltaY, deltaX);
                    var angleInDegrees = ((angleInRads + 2 * Math.PI) % (2 * Math.PI)) / Math.PI * 180;

                    DistanceCache[(leadMinutia.Id, otherMinutia.Id)] = distance;
                    AngleCache[(leadMinutia.Id, otherMinutia.Id)] = angleInDegrees;
                }
            }
        }
    }

    private unsafe void CalculateMetricsAvx2()
    {
        foreach (var group in Groups)
        {
            var filteredMinutiae = group.ToArray();
            int length = filteredMinutiae.Length;

            for (int i = 0; i < length; i++)
            {
                var leadMinutia = filteredMinutiae[i];
                int j = i + 1;

                for (; j + 3 < length; j += 4)
                {
                    Vector256<double> x2 = Vector256.Create(
                        (double)filteredMinutiae[j].X, (double)filteredMinutiae[j + 1].X,
                        (double)filteredMinutiae[j + 2].X, (double)filteredMinutiae[j + 3].X);

                    Vector256<double> y2 = Vector256.Create(
                        (double)filteredMinutiae[j].Y, (double)filteredMinutiae[j + 1].Y,
                        (double)filteredMinutiae[j + 2].Y, (double)filteredMinutiae[j + 3].Y);

                    Vector256<double> x1Vec = Vector256.Create((double)leadMinutia.X);
                    Vector256<double> y1Vec = Vector256.Create((double)leadMinutia.Y);

                    Vector256<double> deltaX = Avx.Subtract(x1Vec, x2);
                    Vector256<double> deltaY = Avx.Subtract(y1Vec, y2);
                    Vector256<double> deltaX2 = Avx.Multiply(deltaX, deltaX);
                    Vector256<double> deltaY2 = Avx.Multiply(deltaY, deltaY);
                    Vector256<double> sum = Avx.Add(deltaX2, deltaY2);
                    Vector256<double> distances = Avx.Sqrt(sum);

                    Span<double> distArray = stackalloc double[4];
                    Span<double> dxArray = stackalloc double[4];
                    Span<double> dyArray = stackalloc double[4];

                    distances.CopyTo(distArray);
                    deltaX.CopyTo(dxArray);
                    deltaY.CopyTo(dyArray);

                    for (int k = 0; k < 4; k++)
                    {
                        var otherId = filteredMinutiae[j + k].Id;
                        DistanceCache[(leadMinutia.Id, otherId)] = distArray[k];

                        var angleInRads = Math.Atan2(dyArray[k], dxArray[k]);
                        var angleInDegrees = ((angleInRads + 2 * Math.PI) % (2 * Math.PI)) / Math.PI * 180;
                        AngleCache[(leadMinutia.Id, otherId)] = angleInDegrees;
                    }
                }

                for (; j < length; j++)
                {
                    var otherMinutia = filteredMinutiae[j];
                    var deltaX = leadMinutia.X - otherMinutia.X;
                    var deltaY = leadMinutia.Y - otherMinutia.Y;
                    var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                    var angleInRads = Math.Atan2(deltaY, deltaX);
                    var angleInDegrees = ((angleInRads + 2 * Math.PI) % (2 * Math.PI)) / Math.PI * 180;

                    DistanceCache[(leadMinutia.Id, otherMinutia.Id)] = distance;
                    AngleCache[(leadMinutia.Id, otherMinutia.Id)] = angleInDegrees;
                }
            }
        }
    }

    private void CalculateMetricsScalar()
    {
        foreach (var group in Groups)
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

                    DistanceCache[(leadMinutia.Id, otherMinutia.Id)] = distance;
                    AngleCache[(leadMinutia.Id, otherMinutia.Id)] = angleInDegrees;
                }
            }
        }
    }

    public void MakeLocalComparison(int maxThreads = -1)
    {
        var options = new ParallelOptions();
        if (maxThreads > 0)
        {
            options.MaxDegreeOfParallelism = maxThreads;
        }
        var groups = Groups.Select(g => g.ToList()).ToList();
        var results = new ConcurrentBag<LocalComparisonResult>();

        Parallel.For(0, groups.Count, options, i =>
        {
            var group1 = groups[i];
            var localResults = new List<LocalComparisonResult>();

            for (int j = i + 1; j < groups.Count; j++)
            {
                var group2 = groups[j];
                Parallel.For(0, group1.Count, options, k =>
                {
                    var minutia1 = group1[k];
                    var others1 = group1.Where(x => x.Id != minutia1.Id).ToArray();

                    for (int l = 0; l < group2.Count; l++)
                    {
                        var minutia2 = group2[l];
                        var others2 = group2.Where(x => x.Id != minutia2.Id).ToArray();

                        // SIMD-оптимізоване обчислення подібності
                        var similarity = GetSquareSimilaritySimd(minutia1, minutia2, others1, others2);

                        if (similarity >= 30)
                        {
                            lock (localResults)
                            {
                                localResults.Add(new LocalComparisonResult(
                                    minutia1.ImageId, minutia2.ImageId,
                                    minutia1.Id, minutia2.Id, similarity));
                                localResults.Add(new LocalComparisonResult(
                                    minutia2.ImageId, minutia1.ImageId,
                                    minutia2.Id, minutia1.Id, similarity));
                            }
                        }
                    }
                });
            }

            foreach (var result in localResults)
                results.Add(result);
        });

        LocalResults = results.ToList();
    }

    /// <summary>
    /// SIMD-оптимізоване обчислення локальної подібності
    /// </summary>
    private double GetSquareSimilaritySimd(Minutia m1, Minutia m2,
        Minutia[] ml1, Minutia[] ml2)
    {
        if (ml1.Length == 0 || ml2.Length == 0) return 0;

        var used1 = new HashSet<int>();
        var used2 = new HashSet<int>();
        var score = 0;
        var maxMatches = Math.Min(ml1.Length, ml2.Length);

        // Векторизоване обчислення конволюцій для всіх пар
        var convolutions = new (int id1, int id2, double conv)[ml1.Length * ml2.Length];
        int convIndex = 0;

        if (Avx2.IsSupported && ml1.Length >= 4 && ml2.Length >= 4)
        {
            // Векторизована обробка пакетами по 4
            for (int i = 0; i + 3 < ml1.Length; i += 4)
            {
                for (int j = 0; j < ml2.Length; j++)
                {
                    var conv = CalculateLocalConvolutionBatch4(m1, m2, ml1, i, ml2[j]);
                    for (int k = 0; k < 4 && i + k < ml1.Length; k++)
                    {
                        if (conv[k] != double.PositiveInfinity)
                        {
                            convolutions[convIndex++] = (ml1[i + k].Id, ml2[j].Id, conv[k]);
                        }
                    }
                }
            }
        }

        // Залишкові та малі списки обробляємо скалярно
        int startI = Avx2.IsSupported ? (ml1.Length / 4) * 4 : 0;
        for (int i = startI; i < ml1.Length; i++)
        {
            for (int j = 0; j < ml2.Length; j++)
            {
                double conv = CalculateLocalConvolution(m1, m2, ml1[i], ml2[j]);
                if (conv != double.PositiveInfinity)
                {
                    convolutions[convIndex++] = (ml1[i].Id, ml2[j].Id, conv);
                }
            }
        }

        // Сортування та жадібне співставлення
        Array.Sort(convolutions, 0, convIndex,
            Comparer<(int, int, double)>.Create((a, b) => a.Item3.CompareTo(b.Item3)));

        for (int i = 0; i < convIndex && score < maxMatches; i++)
        {
            var match = convolutions[i];
            if (!used1.Contains(match.id1) && !used2.Contains(match.id2))
            {
                used1.Add(match.id1);
                used2.Add(match.id2);
                score++;
            }
        }

        return (double)score / maxMatches * 100;
    }

    /// <summary>
    /// Векторизоване обчислення 4 конволюцій одночасно
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe double[] CalculateLocalConvolutionBatch4(Minutia m1, Minutia m2,
        Minutia[] ml1, int startIndex, Minutia m4)
    {
        var results = new double[4];

        // Отримуємо метрики для m2-m4
        var distance2 = GetMetric(DistanceCache, m2.Id, m4.Id);
        var angle2 = GetMetric(AngleCache, m2.Id, m4.Id, true);

        // Векторизоване обчислення для 4 елементів ml1
        Vector256<double> distance2Vec = Vector256.Create(distance2);
        Vector256<double> angle2Vec = Vector256.Create(angle2);
        Vector256<double> threshold7 = Vector256.Create(DISTANCE_THRESHOLD);
        Vector256<double> threshold45 = Vector256.Create(ANGLE_THRESHOLD);

        // Завантажуємо метрики для 4 пар m1-ml1[i]
        Vector256<double> distances1 = Vector256.Create(
            GetMetric(DistanceCache, m1.Id, ml1[startIndex].Id),
            GetMetric(DistanceCache, m1.Id, ml1[startIndex + 1].Id),
            GetMetric(DistanceCache, m1.Id, ml1[startIndex + 2].Id),
            GetMetric(DistanceCache, m1.Id, ml1[startIndex + 3].Id));

        Vector256<double> angles1 = Vector256.Create(
            GetMetric(AngleCache, m1.Id, ml1[startIndex].Id, true),
            GetMetric(AngleCache, m1.Id, ml1[startIndex + 1].Id, true),
            GetMetric(AngleCache, m1.Id, ml1[startIndex + 2].Id, true),
            GetMetric(AngleCache, m1.Id, ml1[startIndex + 3].Id, true));

        // Обчислення різниць
        Vector256<double> diffDistance = Avx.Subtract(distances1, distance2Vec);
        diffDistance = Avx.Max(diffDistance, Avx.Subtract(Vector256<double>.Zero, diffDistance)); // Abs

        Vector256<double> diffAngle = Avx.Subtract(angles1, angle2Vec);
        diffAngle = Avx.Max(diffAngle, Avx.Subtract(Vector256<double>.Zero, diffAngle)); // Abs

        // Перевірка порогів
        Vector256<double> distanceMask = Avx.CompareLessThanOrEqual(diffDistance, threshold7);
        Vector256<double> angleMask = Avx.CompareLessThanOrEqual(diffAngle, threshold45);
        Vector256<double> combinedMask = Avx.And(distanceMask, angleMask);

        // Обчислення конволюції
        Vector256<double> seven = Vector256.Create(7.0);
        Vector256<double> fortyfive = Vector256.Create(45.0);
        Vector256<double> convolution = Avx.Add(
            Avx.Divide(diffDistance, seven),
            Avx.Divide(diffAngle, fortyfive));

        // Застосування маски (відфільтровуємо невалідні)
        Vector256<double> infinity = Vector256.Create(double.PositiveInfinity);
        convolution = Avx.BlendVariable(infinity, convolution, combinedMask);

        Span<double> convArray = stackalloc double[4];
        convolution.CopyTo(convArray);
        convArray.CopyTo(results);

        return results;
    }

    public void MakeGlobalComparison(int maxThreads = -1)
    {
        var options = new ParallelOptions();
        if (maxThreads > 0)
        {
            options.MaxDegreeOfParallelism = maxThreads;
        }
        var groups = LocalResults.GroupBy(x => (x.ImageId1, x.ImageId2)).ToList();
        var results = new ConcurrentBag<GlobalComparisonResult>();

        Parallel.For(0, groups.Count, options, k =>
        {
            var group = groups[k];
            if (group.Key.ImageId1 > group.Key.ImageId2) return;

            var groupElems = group.ToList();
            var m1 = MinutiaeByImageId[group.Key.ImageId1];
            var m2 = MinutiaeByImageId[group.Key.ImageId2];
            var scores = new ConcurrentBag<(int m1, int m2, double score)>();

            Parallel.For(0, groupElems.Count, options, i =>
            {
                var localResult = groupElems[i];
                var shift = ShiftMinutia(group.Key.ImageId2,
                    localResult.TargetMinutiaId1, localResult.TargetMinutiaId2).ToList();

                // SIMD-оптимізоване глобальне порівняння
                var globalResult = CompareGlobalSimd(m1, shift).ToList();
                double score = globalResult.Count / (double)Math.Min(m1.Count, m2.Count) * 100;

                scores.Add((localResult.TargetMinutiaId1, localResult.TargetMinutiaId2, score));

                Parallel.For(0, globalResult.Count, options, j =>
                {
                    var result = globalResult[j];
                    var shift1 = ShiftMinutia(group.Key.ImageId2,
                        result.MinutiaId1, result.MinutiaId2).ToList();
                    var globalResult1 = CompareGlobalSimd(m1, shift1).ToList();
                    double score1 = globalResult1.Count / (double)Math.Min(m1.Count, m2.Count) * 100;

                    scores.Add((result.MinutiaId1, result.MinutiaId2, score1));
                });
            });

            var maxScore = scores.MaxBy(x => x.score);
            results.Add(new GlobalComparisonResult(
                group.Key.ImageId1, group.Key.ImageId2,
                maxScore.score, maxScore.m1, maxScore.m2));
            results.Add(new GlobalComparisonResult(
                group.Key.ImageId2, group.Key.ImageId1,
                maxScore.score, maxScore.m2, maxScore.m1));
        });

        GlobalResults = results.ToList();
    }

    /// <summary>
    /// SIMD-оптимізоване глобальне порівняння
    /// </summary>
    private IEnumerable<MinutiaComparisonResult> CompareGlobalSimd(
        List<Minutia> first, List<Minutia> second)
    {
        if (first.Count == 0 || second.Count == 0) yield break;

        var used1 = new HashSet<int>();
        var used2 = new HashSet<int>();
        var maxMatches = Math.Min(first.Count, second.Count);

        var candidates = new List<MinutiaComparisonResult>();

        // Векторизований пошук кандидатів
        if (Avx2.IsSupported)
        {
            for (int i = 0; i < first.Count; i++)
            {
                var m1 = first[i];
                int j = 0;

                // Обробка пакетами по 4
                for (; j + 3 < second.Count; j += 4)
                {
                    var results = CompareGlobalBatch4(m1, second, j);
                    foreach (var result in results)
                    {
                        if (result.Score < 3.0) // Валідний кандидат
                        {
                            candidates.Add(result);
                        }
                    }
                }

                // Залишкові
                for (; j < second.Count; j++)
                {
                    var m2 = second[j];
                    var deltaX = m1.X - m2.X;
                    var deltaY = m1.Y - m2.Y;
                    var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

                    if (distance > GLOBAL_DISTANCE_DELTA) continue;

                    var featureAngleDiff = (Math.Abs(m1.Theta - m2.Theta) + 2 * Math.PI) %
                        (2 * Math.PI) / Math.PI * 180;

                    if (featureAngleDiff > GLOBAL_ANGLE_DELTA) continue;

                    var score = distance / GLOBAL_DISTANCE_DELTA +
                        featureAngleDiff / GLOBAL_ANGLE_DELTA +
                        (m1.IsTermination != m2.IsTermination ? 1 : 0);

                    candidates.Add(new MinutiaComparisonResult(m1.Id, m2.Id, score));
                }
            }
        }
        else
        {
            // Fallback на скалярну версію
            candidates.AddRange(CompareGlobalScalar(first, second));
        }

        // Сортування та жадібний вибір
        candidates.Sort((a, b) => a.Score.CompareTo(b.Score));

        foreach (var result in candidates)
        {
            if (used1.Count >= maxMatches || used2.Count >= maxMatches) break;

            if (!used1.Contains(result.MinutiaId1) && !used2.Contains(result.MinutiaId2))
            {
                used1.Add(result.MinutiaId1);
                used2.Add(result.MinutiaId2);
                yield return result;
            }
        }
    }

    /// <summary>
    /// Векторизоване порівняння однієї мінуції з 4 іншими
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe MinutiaComparisonResult[] CompareGlobalBatch4(Minutia m1,
        List<Minutia> second, int startIndex)
    {
        var results = new List<MinutiaComparisonResult>(4);

        Vector256<double> x1 = Vector256.Create((double)m1.X);
        Vector256<double> y1 = Vector256.Create((double)m1.Y);
        Vector256<double> theta1 = Vector256.Create(m1.Theta);

        Vector256<double> x2 = Vector256.Create(
            (double)second[startIndex].X, (double)second[startIndex + 1].X,
            (double)second[startIndex + 2].X, (double)second[startIndex + 3].X);

        Vector256<double> y2 = Vector256.Create(
            (double)second[startIndex].Y, (double)second[startIndex + 1].Y,
            (double)second[startIndex + 2].Y, (double)second[startIndex + 3].Y);

        Vector256<double> deltaX = Avx.Subtract(x1, x2);
        Vector256<double> deltaY = Avx.Subtract(y1, y2);
        Vector256<double> deltaX2 = Avx.Multiply(deltaX, deltaX);
        Vector256<double> deltaY2 = Avx.Multiply(deltaY, deltaY);
        Vector256<double> distances = Avx.Sqrt(Avx.Add(deltaX2, deltaY2));

        Span<double> distArray = stackalloc double[4];
        distances.CopyTo(distArray);

        for (int i = 0; i < 4 && startIndex + i < second.Count; i++)
        {
            var distance = distArray[i];
            if (distance > GLOBAL_DISTANCE_DELTA) continue;

            var m2 = second[startIndex + i];
            var featureAngleDiff = (Math.Abs(m1.Theta - m2.Theta) + 2 * Math.PI) %
                (2 * Math.PI) / Math.PI * 180;

            if (featureAngleDiff > GLOBAL_ANGLE_DELTA) continue;

            var score = distance / GLOBAL_DISTANCE_DELTA +
                featureAngleDiff / GLOBAL_ANGLE_DELTA +
                (m1.IsTermination != m2.IsTermination ? 1 : 0);

            results.Add(new MinutiaComparisonResult(m1.Id, m2.Id, score));
        }

        return results.ToArray();
    }

    private IEnumerable<MinutiaComparisonResult> CompareGlobalScalar(
        List<Minutia> first, List<Minutia> second)
    {
        foreach (var m1 in first)
        {
            foreach (var m2 in second)
            {
                var deltaX = m1.X - m2.X;
                var deltaY = m1.Y - m2.Y;
                var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

                if (distance > GLOBAL_DISTANCE_DELTA) continue;

                var featureAngleDiff = (Math.Abs(m1.Theta - m2.Theta) + 2 * Math.PI) %
                    (2 * Math.PI) / Math.PI * 180;

                if (featureAngleDiff > GLOBAL_ANGLE_DELTA) continue;

                var score = distance / GLOBAL_DISTANCE_DELTA +
                    featureAngleDiff / GLOBAL_ANGLE_DELTA +
                    (m1.IsTermination != m2.IsTermination ? 1 : 0);

                yield return new MinutiaComparisonResult(m1.Id, m2.Id, score);
            }
        }
    }

    private Dictionary<int, (double X, double Y)> GetSquares()
    {
        var sums = new Dictionary<int, (double sumX, double sumY, int count)>();

        foreach (var m in Minutiae)
        {
            if (!sums.TryGetValue(m.ImageId, out var data))
                data = (0, 0, 0);

            sums[m.ImageId] = (data.sumX + m.X, data.sumY + m.Y, data.count + 1);
        }

        var result = new Dictionary<int, (double X, double Y)>();
        foreach (var kvp in sums)
        {
            result[kvp.Key] = (kvp.Value.sumX / kvp.Value.count,
                              kvp.Value.sumY / kvp.Value.count);
        }
        return result;
    }

    private IEnumerable<IGrouping<int, Minutia>> GetMinutiaeInSquares(
        Dictionary<int, (double X, double Y)> squares)
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
                return g.Where(m => m.X >= minX && m.X <= maxX &&
                                   m.Y >= minY && m.Y <= maxY)
                        .AsGrouping(g.Key);
            })
            .Where(g => g.Any());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double GetMetric(IDictionary<(int Id1, int Id2), double> metrics,
        int Id1, int Id2, bool isAngle = false)
    {
        if (metrics.TryGetValue((Id1, Id2), out var metric))
            return metric;

        if (metrics.TryGetValue((Id2, Id1), out var reversedMetric))
            return isAngle ? 360 - reversedMetric : reversedMetric;

        return double.NaN;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double CalculateLocalConvolution(Minutia m1, Minutia m2, Minutia m3, Minutia m4)
    {
        var distance1 = GetMetric(DistanceCache, m1.Id, m3.Id);
        var distance2 = GetMetric(DistanceCache, m2.Id, m4.Id);
        var diffDistance = Math.Abs(distance1 - distance2);
        if (diffDistance > DISTANCE_THRESHOLD) return double.PositiveInfinity;

        var angle1 = GetMetric(AngleCache, m1.Id, m3.Id, true);
        var angle2 = GetMetric(AngleCache, m2.Id, m4.Id, true);
        var diffAngle = Math.Abs(angle1 - angle2);
        if (diffAngle > ANGLE_THRESHOLD) return double.PositiveInfinity;

        return diffDistance / DISTANCE_THRESHOLD + diffAngle / ANGLE_THRESHOLD;
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