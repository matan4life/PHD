var hybridBuckets = File.ReadAllLines("buckets_hybrid.txt")
           .Select(int.Parse)
           .ToArray();

var scalarBuckets = File.ReadAllLines("buckets_scalar.txt")
    .Select(int.Parse)
    .ToArray();

// Анализ
var hybridMetrics = AnalyzeBuckets(hybridBuckets);
var scalarMetrics = AnalyzeBuckets(scalarBuckets);

// Вывод результатов
Console.WriteLine("=== Bucket Distribution Analysis ===\n");
Console.WriteLine($"{"Metric",-30} {"Hybrid",-20} {"Scalar",-20} {"Ratio (S/H)",-15}");
Console.WriteLine(new string('-', 90));

foreach (var metric in hybridMetrics.Keys)
{
    var hybrid = hybridMetrics[metric];
    var scalar = scalarMetrics[metric];
    var ratio = hybrid != 0 ? scalar / hybrid : double.NaN;

    Console.WriteLine($"{metric,-30} {hybrid,-20:F4} {scalar,-20:F4} {ratio,-15:F4}");
}

// Сохранение в CSV
using (var writer = new StreamWriter("bucket_analysis.csv"))
{
    writer.WriteLine("Metric,Hybrid,Scalar,Ratio");
    foreach (var metric in hybridMetrics.Keys)
    {
        var hybrid = hybridMetrics[metric];
        var scalar = scalarMetrics[metric];
        var ratio = hybrid != 0 ? scalar / hybrid : double.NaN;
        writer.WriteLine($"{metric},{hybrid},{scalar},{ratio}");
    }
}

Console.WriteLine("\nResults saved to bucket_analysis.csv");

static Dictionary<string, double> AnalyzeBuckets(int[] buckets)
{
    var nonZero = buckets.Where(b => b != 0).ToArray();
    var results = new Dictionary<string, double>();

    // Базовая статистика
    var mean = buckets.Average();
    results["Mean"] = mean;
    results["StdDev"] = Math.Sqrt(buckets.Average(v => Math.Pow(v - mean, 2)));
    results["CV"] = mean != 0 ? results["StdDev"] / mean : 0;
    results["Median"] = Percentile(buckets, 0.5);
    results["Q1"] = Percentile(buckets, 0.25);
    results["Q3"] = Percentile(buckets, 0.75);
    results["IQR"] = results["Q3"] - results["Q1"];

    // Равномерность
    results["EmptyBuckets%"] = buckets.Count(b => b == 0) / (double)buckets.Length * 100;
    results["NonEmptyBuckets%"] = 100 - results["EmptyBuckets%"];
    results["Entropy"] = CalculateEntropy(buckets);
    results["ChiSquare"] = CalculateChiSquare(buckets);

    // Автокорреляция (вместо неправильного Moran's I)
    results["Lag1Autocorrelation"] = CalculateLag1Autocorr(buckets);

    // Дисперсия (вместо Gini)
    results["IndexOfDispersion"] = results["StdDev"] * results["StdDev"] / mean;
    results["CoefficientOfDispersion"] = results["StdDev"] * results["StdDev"] / mean;

    // Скачкообразность
    results["MeanAbsConsecutiveDiff"] = CalculateMeanAbsDiff(buckets);
    results["MaxConsecutiveDiff"] = CalculateMaxConsecutiveDiff(buckets);

    // Коллизии
    results["AvgChainLength"] = nonZero.Length > 0 ? nonZero.Average() : 0;
    results["MaxChainLength"] = buckets.Max();
    results["P95ChainLength"] = nonZero.Length > 0 ? Percentile(nonZero, 0.95) : 0;
    results["P99ChainLength"] = nonZero.Length > 0 ? Percentile(nonZero, 0.99) : 0;

    // Load factor
    results["LoadFactor"] = buckets.Sum() / (double)buckets.Length;

    return results;
}

static double CalculateEntropy(int[] buckets)
{
    var total = buckets.Sum();
    if (total == 0) return 0;

    return -buckets.Where(b => b > 0)
        .Sum(b => {
            var p = b / (double)total;
            return p * Math.Log(p, 2);
        });
}

static double CalculateChiSquare(int[] buckets)
{
    var expected = buckets.Average();
    if (expected == 0) return 0;
    return buckets.Sum(b => Math.Pow(b - expected, 2) / expected);
}

// Правильная формула для lag-1 автокорреляции
static double CalculateLag1Autocorr(int[] buckets)
{
    if (buckets.Length < 2) return 0;

    var mean = buckets.Average();
    var variance = buckets.Average(x => Math.Pow(x - mean, 2));

    if (variance == 0) return 0;

    var covariance = 0.0;
    for (int i = 0; i < buckets.Length - 1; i++)
    {
        covariance += (buckets[i] - mean) * (buckets[i + 1] - mean);
    }
    covariance /= (buckets.Length - 1);

    return covariance / variance;
}

static double CalculateMeanAbsDiff(int[] buckets)
{
    if (buckets.Length <= 1) return 0;

    var sum = 0.0;
    for (int i = 0; i < buckets.Length - 1; i++)
    {
        sum += Math.Abs(buckets[i + 1] - buckets[i]);
    }
    return sum / (buckets.Length - 1);
}

static double CalculateMaxConsecutiveDiff(int[] buckets)
{
    if (buckets.Length <= 1) return 0;

    var max = 0.0;
    for (int i = 0; i < buckets.Length - 1; i++)
    {
        var diff = Math.Abs(buckets[i + 1] - buckets[i]);
        if (diff > max) max = diff;
    }
    return max;
}

static double Percentile(int[] sequence, double percentile)
{
    if (sequence.Length == 0) return 0;

    var sorted = sequence.OrderBy(x => x).ToArray();
    var index = percentile * (sorted.Length - 1);
    var lower = (int)Math.Floor(index);
    var upper = (int)Math.Ceiling(index);
    var weight = index - lower;

    return sorted[lower] * (1 - weight) + sorted[upper] * weight;
}

static string GetDescription(string metric)
{
    return metric switch
    {
        "Mean" => "Среднее значение индексов в бакетах",
        "StdDev" => "Стандартное отклонение индексов",
        "CV" => "Коэффициент вариации (StdDev/Mean)",
        "Median" => "Медиана распределения индексов",
        "Q1" => "Первый квартиль (25-й процентиль)",
        "Q3" => "Третий квартиль (75-й процентиль)",
        "IQR" => "Интерквартильный размах (Q3-Q1)",
        "EmptyBuckets%" => "Процент пустых бакетов",
        "NonEmptyBuckets%" => "Процент непустых бакетов",
        "Entropy" => "Энтропия Шеннона распределения",
        "ChiSquare" => "Хи-квадрат статистика отклонения от равномерности",
        "Lag1Autocorrelation" => "Автокорреляция первого порядка (соседние элементы)",
        "IndexOfDispersion" => "Индекс дисперсии (Var/Mean)",
        "CoefficientOfDispersion" => "Коэффициент дисперсии (Var/Mean)",
        "MeanAbsConsecutiveDiff" => "Средняя абсолютная разность между соседними бакетами",
        "MaxConsecutiveDiff" => "Максимальная разность между соседними бакетами",
        "AvgChainLength" => "Средняя длина цепочки коллизий",
        "MaxChainLength" => "Максимальная длина цепочки коллизий",
        "P95ChainLength" => "95-й процентиль длины цепочки",
        "P99ChainLength" => "99-й процентиль длины цепочки",
        "LoadFactor" => "Фактор загрузки хеш-таблицы",
        _ => ""
    };
}

record LocalComparisonResult(int ImageId1, int ImageId2, int TargetMinutiaId1, int TargetMinutiaId2, double Score);
record GlobalComparisonResult(int ImageId1, int ImageId2, double Score, int MinutiaId1, int MinutiaId2);
record MinutiaComparisonResult(int MinutiaId1, int MinutiaId2, double Score);