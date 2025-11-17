//////// See https://aka.ms/new-console-template for more information
//using Demo;
//using Demo.Database;
//using Demo.Entities;
//using Microsoft.EntityFrameworkCore;
//using System.Diagnostics;
//using System.Runtime.Intrinsics.X86;
//using System.Text;

//Console.WriteLine("=== Advanced Fingerprint Matching Experiments ===\n");

//// Check CPU capabilities
//PrintCpuCapabilities();

//var context = new FingerprintContext();

//var images = await context.Images
//    .Include(x => x.Minutiae)
//    .AsNoTracking()
//    .ToListAsync();

//var minutiae = images.SelectMany(x => x.Minutiae).ToList();

//Console.WriteLine($"Total images: {images.Count}");
//Console.WriteLine($"Total minutiae: {minutiae.Count}");
//Console.WriteLine($"Max groups available: {images.Count / 8}");
//Console.WriteLine();

//// Run all experiments
////await RunThreadDependencyExperiment([.. minutiae.Where(x => x.ImageId < 33)]);
//await RunDatasetSizeExperiment(images);
////await RunProfilingExperiment(minutiae);

//Console.WriteLine("\nAll experiments completed. Press any key to exit...");
//Console.ReadKey();

//static void PrintCpuCapabilities()
//{
//    Console.WriteLine("CPU Capabilities:");
//    Console.WriteLine($"  Processor Count: {Environment.ProcessorCount}");
//    Console.WriteLine($"  SSE4.2: {Sse42.IsSupported}");
//    Console.WriteLine($"  AVX: {Avx.IsSupported}");
//    Console.WriteLine($"  AVX2: {Avx2.IsSupported}");
//    Console.WriteLine($"  AVX-512F: {Avx512F.IsSupported}");
//    Console.WriteLine();
//}

//static async Task RunThreadDependencyExperiment(List<Minutia> testData)
//{
//    Console.WriteLine($"=== Thread Dependency Experiment ({testData.GroupBy(x => x.ImageId).Count()} images, {testData.Count} minutiae) ===\n");

//    var threadCounts = new List<int>();
//    for (int i = 1; i <= Environment.ProcessorCount; i++)
//    {
//        threadCounts.Add(i);
//    }

//    var results = new List<ThreadExperimentResult>();

//    foreach (int threadCount in threadCounts)
//    {
//        Console.WriteLine($"Testing with {threadCount} threads...");

//        // Test ParallellStepFunction
//        var parallelResult = await TestImplementationWithThreads(
//            "ParallellStepFunction",
//            () => new ParallellStepFunction(testData),
//            threadCount,
//            3);

//        // Test SimdParallelStepFunction
//        var simdResult = await TestImplementationWithThreads(
//            "SimdParallelStepFunction",
//            () => new SimdParallelStepFunction(testData),
//            threadCount,
//            3);

//        // Test HybridSimdStepFunction
//        var hybridResult = await TestImplementationWithThreads(
//            "HybridSimdStepFunction",
//            () => new HybridSimdStepFunction(testData),
//            threadCount,
//            3);

//        results.Add(new ThreadExperimentResult(threadCount, parallelResult, simdResult, hybridResult));

//        Console.WriteLine($"  Parallel: {parallelResult.TotalTime:F2} ms");
//        Console.WriteLine($"  SIMD: {simdResult.TotalTime:F2} ms");
//        Console.WriteLine($"  Hybrid: {hybridResult.TotalTime:F2} ms");
//        Console.WriteLine();
//    }

//    await SaveThreadExperimentResults(results);
//    PrintThreadExperimentSummary(results);
//}

//static async Task RunDatasetSizeExperiment(List<Image> allImages)
//{
//    Console.WriteLine("=== Dataset Size Dependency Experiment ===\n");

//    int maxGroups = Math.Min(10, allImages.Count / 8);
//    var results = new List<DatasetSizeResult>();

//    for (int groupCount = 3; groupCount <= 3; groupCount++)
//    {
//        // Take first N groups (N * 8 images)
//        var selectedImages = allImages.Take(groupCount * 8).ToList();
//        var minutiae = selectedImages.SelectMany(x => x.Minutiae).ToList();

//        Console.WriteLine($"Testing with {groupCount} groups ({selectedImages.Count} images, {minutiae.Count} minutiae)...");

//        // Test all three implementations
//        //var parallelResult = BenchmarkImplementation(
//        //    "ParallellStepFunction",
//        //    () => new ParallellStepFunction(minutiae),
//        //    iterations: 1);

//        //var simdResult = BenchmarkImplementation(
//        //    "SimdParallelStepFunction",
//        //    () => new SimdParallelStepFunction(minutiae),
//        //    iterations: 3);

//        var hybridResult = BenchmarkImplementation(
//            "HybridSimdStepFunction",
//            () => new HybridSimdStepFunction(minutiae),
//            iterations: 1);

//        //results.Add(new DatasetSizeResult(
//        //    groupCount,
//        //    selectedImages.Count,
//        //    minutiae.Count,
//        //    parallelResult,
//        //    default,
//        //    default));
//        //    //simdResult, 
//        //    //hybridResult));

//        Console.WriteLine($"  Images: {selectedImages.Count}, Minutiae: {minutiae.Count}");
//        //Console.WriteLine($"  Parallel: {parallelResult.total:F2} ms");
//        //Console.WriteLine($"  SIMD: {simdResult.total:F2} ms");
//        Console.WriteLine($"  Hybrid: {hybridResult.total:F2} ms");
//        Console.WriteLine();
//    }

//    await SaveDatasetSizeResults(results);
//    PrintDatasetSizeExperimentSummary(results);
//}

//static async Task RunProfilingExperiment(List<Minutia> testData)
//{
//    Console.WriteLine("=== Detailed Profiling Experiment ===\n");

//    var implementations = new Dictionary<string, Func<IStepFunction>>
//    {
//        ["ParallellStepFunction"] = () => new ParallellStepFunction(testData),
//        ["SimdParallelStepFunction"] = () => new SimdParallelStepFunction(testData),
//        ["HybridSimdStepFunction"] = () => new HybridSimdStepFunction(testData)
//    };

//    var profilingResults = new List<ProfilingResult>();

//    foreach (var kvp in implementations)
//    {
//        Console.WriteLine($"Profiling {kvp.Key}...");

//        var result = DetailedProfiling(kvp.Key, kvp.Value, iterations: 5);
//        profilingResults.Add(result);

//        Console.WriteLine($"  AdjustCoords: {result.AdjustCoords:F2} ± {result.AdjustCoordsStdDev:F2} ms");
//        Console.WriteLine($"  CalculateMetrics: {result.CalculateMetrics:F2} ± {result.CalculateMetricsStdDev:F2} ms");
//        Console.WriteLine($"  MakeLocalComparison: {result.LocalComparison:F2} ± {result.LocalComparisonStdDev:F2} ms");
//        Console.WriteLine($"  MakeGlobalComparison: {result.GlobalComparison:F2} ± {result.GlobalComparisonStdDev:F2} ms");
//        Console.WriteLine($"  Total: {result.Total:F2} ± {result.TotalStdDev:F2} ms");
//        Console.WriteLine();
//    }

//    await SaveProfilingResults(profilingResults);
//    PrintProfilingSummary(profilingResults);
//}

//static Task<BenchmarkResult> TestImplementationWithThreads(
//    string name,
//    Func<IStepFunction> factory,
//    int threadCount,
//    int iterations)
//{
//    var results = new List<BenchmarkResult>();

//    for (int i = 0; i < iterations; i++)
//    {
//        var impl = factory();
//        var sw = Stopwatch.StartNew();

//        sw.Restart();
//        impl.AdjustCoords();
//        var adjustTime = sw.Elapsed.TotalMilliseconds;

//        sw.Restart();
//        impl.CalculateMetrics();
//        var calculateTime = sw.Elapsed.TotalMilliseconds;

//        sw.Restart();
//        impl.MakeLocalComparison(threadCount);
//        var localTime = sw.Elapsed.TotalMilliseconds;

//        sw.Restart();
//        impl.MakeGlobalComparison(threadCount);
//        var globalTime = sw.Elapsed.TotalMilliseconds;

//        var totalTime = adjustTime + calculateTime + localTime + globalTime;

//        results.Add(new BenchmarkResult(adjustTime, calculateTime, localTime, globalTime, totalTime));
//    }

//    return Task.FromResult(new BenchmarkResult(
//        results.Average(r => r.AdjustCoords),
//        results.Average(r => r.CalculateMetrics),
//        results.Average(r => r.LocalComparison),
//        results.Average(r => r.GlobalComparison),
//        results.Average(r => r.TotalTime)
//    ));
//}

//static (double adjustCoords, double calculateMetrics, double localComparison,
//        double globalComparison, double total, IStepFunction? impl)
//    BenchmarkImplementation(string name, Func<IStepFunction> factory, int iterations)
//{
//    var adjustTimes = new List<double>();
//    var calculateTimes = new List<double>();
//    var localTimes = new List<double>();
//    var globalTimes = new List<double>();
//    var totalTimes = new List<double>();
//    IStepFunction? lastImpl = null;

//    for (int i = 0; i < iterations; i++)
//    {
//        var impl = factory();
//        var sw = Stopwatch.StartNew();

//        sw.Restart();
//        impl.AdjustCoords();
//        adjustTimes.Add(sw.Elapsed.TotalMilliseconds);

//        sw.Restart();
//        impl.CalculateMetrics();
//        calculateTimes.Add(sw.Elapsed.TotalMilliseconds);

//        sw.Restart();
//        impl.MakeLocalComparison();
//        localTimes.Add(sw.Elapsed.TotalMilliseconds);

//        sw.Restart();
//        impl.MakeGlobalComparison();
//        globalTimes.Add(sw.Elapsed.TotalMilliseconds);

//        totalTimes.Add(adjustTimes[i] + calculateTimes[i] + localTimes[i] + globalTimes[i]);
//        lastImpl = impl;
//    }

//    return (adjustTimes.Average(), calculateTimes.Average(), localTimes.Average(),
//           globalTimes.Average(), totalTimes.Average(), lastImpl);
//}

//static ProfilingResult DetailedProfiling(string name, Func<IStepFunction> factory, int iterations)
//{
//    var adjustTimes = new List<double>();
//    var calculateTimes = new List<double>();
//    var localTimes = new List<double>();
//    var globalTimes = new List<double>();
//    var totalTimes = new List<double>();

//    for (int i = 0; i < iterations; i++)
//    {
//        var impl = factory();
//        var sw = Stopwatch.StartNew();

//        // Warm-up for first iteration
//        if (i == 0)
//        {
//            impl.AdjustCoords();
//            impl.CalculateMetrics();
//            impl = factory(); // Reset for actual measurement
//        }

//        sw.Restart();
//        impl.AdjustCoords();
//        adjustTimes.Add(sw.Elapsed.TotalMilliseconds);

//        sw.Restart();
//        impl.CalculateMetrics();
//        calculateTimes.Add(sw.Elapsed.TotalMilliseconds);

//        sw.Restart();
//        impl.MakeLocalComparison();
//        localTimes.Add(sw.Elapsed.TotalMilliseconds);

//        sw.Restart();
//        impl.MakeGlobalComparison();
//        globalTimes.Add(sw.Elapsed.TotalMilliseconds);

//        totalTimes.Add(adjustTimes[i] + calculateTimes[i] + localTimes[i] + globalTimes[i]);
//    }

//    return new ProfilingResult(
//        name,
//        adjustTimes.Average(), StdDev(adjustTimes),
//        calculateTimes.Average(), StdDev(calculateTimes),
//        localTimes.Average(), StdDev(localTimes),
//        globalTimes.Average(), StdDev(globalTimes),
//        totalTimes.Average(), StdDev(totalTimes)
//    );
//}

//static async Task SaveThreadExperimentResults(List<ThreadExperimentResult> results)
//{
//    var csv = new StringBuilder();
//    csv.AppendLine("ThreadCount,Implementation,AdjustCoords,CalculateMetrics,LocalComparison,GlobalComparison,Total");

//    foreach (var result in results)
//    {
//        csv.AppendLine($"{result.ThreadCount},Parallel,{result.Parallel.AdjustCoords:F2},{result.Parallel.CalculateMetrics:F2},{result.Parallel.LocalComparison:F2},{result.Parallel.GlobalComparison:F2},{result.Parallel.TotalTime:F2}");
//        csv.AppendLine($"{result.ThreadCount},SIMD,{result.Simd.AdjustCoords:F2},{result.Simd.CalculateMetrics:F2},{result.Simd.LocalComparison:F2},{result.Simd.GlobalComparison:F2},{result.Simd.TotalTime:F2}");
//        csv.AppendLine($"{result.ThreadCount},Hybrid,{result.Hybrid.AdjustCoords:F2},{result.Hybrid.CalculateMetrics:F2},{result.Hybrid.LocalComparison:F2},{result.Hybrid.GlobalComparison:F2},{result.Hybrid.TotalTime:F2}");
//    }

//    await File.WriteAllTextAsync("thread_dependency_results.csv", csv.ToString());
//    Console.WriteLine("Thread dependency results saved to thread_dependency_results.csv");
//}

//static async Task SaveDatasetSizeResults(List<DatasetSizeResult> results)
//{
//    var csv = new StringBuilder();
//    csv.AppendLine("Groups,Images,Minutiae,Implementation,AdjustCoords,CalculateMetrics,LocalComparison,GlobalComparison,Total");

//    foreach (var result in results)
//    {
//        csv.AppendLine($"{result.Groups},{result.Images},{result.Minutiae},Parallel,{result.Parallel.adjustCoords:F2},{result.Parallel.calculateMetrics:F2},{result.Parallel.localComparison:F2},{result.Parallel.globalComparison:F2},{result.Parallel.total:F2}");
//        csv.AppendLine($"{result.Groups},{result.Images},{result.Minutiae},SIMD,{result.Simd.adjustCoords:F2},{result.Simd.calculateMetrics:F2},{result.Simd.localComparison:F2},{result.Simd.globalComparison:F2},{result.Simd.total:F2}");
//        csv.AppendLine($"{result.Groups},{result.Images},{result.Minutiae},Hybrid,{result.Hybrid.adjustCoords:F2},{result.Hybrid.calculateMetrics:F2},{result.Hybrid.localComparison:F2},{result.Hybrid.globalComparison:F2},{result.Hybrid.total:F2}");
//    }

//    await File.WriteAllTextAsync("dataset_size_results.csv", csv.ToString());
//    Console.WriteLine("Dataset size results saved to dataset_size_results.csv");
//}

//static async Task SaveProfilingResults(List<ProfilingResult> results)
//{
//    var csv = new StringBuilder();
//    csv.AppendLine("Implementation,Component,AverageTime,StdDev");

//    foreach (var result in results)
//    {
//        csv.AppendLine($"{result.Name},AdjustCoords,{result.AdjustCoords:F2},{result.AdjustCoordsStdDev:F2}");
//        csv.AppendLine($"{result.Name},CalculateMetrics,{result.CalculateMetrics:F2},{result.CalculateMetricsStdDev:F2}");
//        csv.AppendLine($"{result.Name},LocalComparison,{result.LocalComparison:F2},{result.LocalComparisonStdDev:F2}");
//        csv.AppendLine($"{result.Name},GlobalComparison,{result.GlobalComparison:F2},{result.GlobalComparisonStdDev:F2}");
//        csv.AppendLine($"{result.Name},Total,{result.Total:F2},{result.TotalStdDev:F2}");
//    }

//    await File.WriteAllTextAsync("profiling_results.csv", csv.ToString());
//    Console.WriteLine("Profiling results saved to profiling_results.csv");
//}

//static void PrintThreadExperimentSummary(List<ThreadExperimentResult> results)
//{
//    Console.WriteLine("\n=== Thread Dependency Summary ===");

//    var bestParallel = results.MinBy(r => r.Parallel.TotalTime);
//    var bestSimd = results.MinBy(r => r.Simd.TotalTime);
//    var bestHybrid = results.MinBy(r => r.Hybrid.TotalTime);

//    Console.WriteLine($"Best performance:");
//    Console.WriteLine($"  Parallel: {bestParallel?.ThreadCount} threads ({bestParallel?.Parallel.TotalTime:F2} ms)");
//    Console.WriteLine($"  SIMD: {bestSimd?.ThreadCount} threads ({bestSimd?.Simd.TotalTime:F2} ms)");
//    Console.WriteLine($"  Hybrid: {bestHybrid?.ThreadCount} threads ({bestHybrid?.Hybrid.TotalTime:F2} ms)");

//    // Calculate scalability
//    var parallelBaseline = results.First(r => r.ThreadCount == 1).Parallel.TotalTime;
//    var simdBaseline = results.First(r => r.ThreadCount == 1).Simd.TotalTime;
//    var hybridBaseline = results.First(r => r.ThreadCount == 1).Hybrid.TotalTime;

//    Console.WriteLine($"\nScalability (vs single thread):");
//    Console.WriteLine($"  Parallel: {parallelBaseline / bestParallel!.Parallel.TotalTime:F2}x speedup");
//    Console.WriteLine($"  SIMD: {simdBaseline / bestSimd!.Simd.TotalTime:F2}x speedup");
//    Console.WriteLine($"  Hybrid: {hybridBaseline / bestHybrid!.Hybrid.TotalTime:F2}x speedup");
//}

//static void PrintDatasetSizeExperimentSummary(List<DatasetSizeResult> results)
//{
//    Console.WriteLine("\n=== Dataset Size Dependency Summary ===");

//    Console.WriteLine("Complexity analysis (time vs dataset size):");

//    foreach (var impl in new[] { "Parallel", "SIMD", "Hybrid" })
//    {
//        Console.WriteLine($"\n{impl} Implementation:");
//        for (int i = 1; i < results.Count; i++)
//        {
//            var prev = results[i - 1];
//            var curr = results[i];

//            double prevTime = impl switch
//            {
//                "Parallel" => prev.Parallel.total,
//                "SIMD" => prev.Simd.total,
//                "Hybrid" => prev.Hybrid.total,
//                _ => 0
//            };

//            double currTime = impl switch
//            {
//                "Parallel" => curr.Parallel.total,
//                "SIMD" => curr.Simd.total,
//                "Hybrid" => curr.Hybrid.total,
//                _ => 0
//            };

//            var timeRatio = currTime / prevTime;
//            var sizeRatio = (double)curr.Minutiae / prev.Minutiae;

//            Console.WriteLine($"  Groups {prev.Groups} -> {curr.Groups}: {timeRatio:F2}x time, {sizeRatio:F2}x data");
//        }
//    }
//}

//static void PrintProfilingSummary(List<ProfilingResult> results)
//{
//    Console.WriteLine("\n=== Profiling Summary ===");

//    Console.WriteLine("Time distribution by component:");
//    foreach (var result in results)
//    {
//        var total = result.Total;
//        Console.WriteLine($"\n{result.Name}:");
//        Console.WriteLine($"  AdjustCoords: {result.AdjustCoords / total * 100:F1}% ({result.AdjustCoords:F2} ms)");
//        Console.WriteLine($"  CalculateMetrics: {result.CalculateMetrics / total * 100:F1}% ({result.CalculateMetrics:F2} ms)");
//        Console.WriteLine($"  LocalComparison: {result.LocalComparison / total * 100:F1}% ({result.LocalComparison:F2} ms)");
//        Console.WriteLine($"  GlobalComparison: {result.GlobalComparison / total * 100:F1}% ({result.GlobalComparison:F2} ms)");
//    }

//    // Component comparison
//    Console.WriteLine("\nComponent performance comparison:");
//    var baseline = results.First(r => r.Name == "ParallellStepFunction");

//    foreach (var result in results.Where(r => r.Name != "ParallellStepFunction"))
//    {
//        Console.WriteLine($"\n{result.Name} vs ParallellStepFunction:");
//        Console.WriteLine($"  AdjustCoords: {baseline.AdjustCoords / result.AdjustCoords:F2}x speedup");
//        Console.WriteLine($"  CalculateMetrics: {baseline.CalculateMetrics / result.CalculateMetrics:F2}x speedup");
//        Console.WriteLine($"  LocalComparison: {baseline.LocalComparison / result.LocalComparison:F2}x speedup");
//        Console.WriteLine($"  GlobalComparison: {baseline.GlobalComparison / result.GlobalComparison:F2}x speedup");
//        Console.WriteLine($"  Total: {baseline.Total / result.Total:F2}x speedup");
//    }
//}

//static double StdDev(IEnumerable<double> values)
//{
//    var avg = values.Average();
//    var sumOfSquares = values.Sum(v => (v - avg) * (v - avg));
//    return Math.Sqrt(sumOfSquares / values.Count());
//}

//// Data structures for experiment results
//record BenchmarkResult(double AdjustCoords, double CalculateMetrics, double LocalComparison, double GlobalComparison, double TotalTime);

//record ThreadExperimentResult(int ThreadCount, BenchmarkResult Parallel, BenchmarkResult Simd, BenchmarkResult Hybrid);

//record DatasetSizeResult(int Groups, int Images, int Minutiae,
//    (double adjustCoords, double calculateMetrics, double localComparison, double globalComparison, double total, IStepFunction impl) Parallel,
//    (double adjustCoords, double calculateMetrics, double localComparison, double globalComparison, double total, IStepFunction impl) Simd,
//    (double adjustCoords, double calculateMetrics, double localComparison, double globalComparison, double total, IStepFunction impl) Hybrid);

//record ProfilingResult(string Name,
//    double AdjustCoords, double AdjustCoordsStdDev,
//    double CalculateMetrics, double CalculateMetricsStdDev,
//    double LocalComparison, double LocalComparisonStdDev,
//    double GlobalComparison, double GlobalComparisonStdDev,
//    double Total, double TotalStdDev);

//record LocalComparisonResult(int ImageId1, int ImageId2, int TargetMinutiaId1, int TargetMinutiaId2, double Score);
//record GlobalComparisonResult(int ImageId1, int ImageId2, double Score, int MinutiaId1, int MinutiaId2);
//record MinutiaComparisonResult(int MinutiaId1, int MinutiaId2, double Score);
