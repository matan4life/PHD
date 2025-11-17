using Microsoft.Extensions.DependencyInjection;
using POC.Python;
using POC.Services;

namespace POC.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPocServices(this IServiceCollection services)
    {
        return services.AddSingleton<IPythonExecutor, PythonExecutor>()
            .AddSingleton<IImageSizeExtractor, ImageSizeExtractor>()
            .AddSingleton<IClusteringVerificationService, ForelClusteringVerificationService>()
            .AddSingleton<IQualityEvaluator, ScoreFunctionClusteringQualityEvaluator>()
            .AddSingleton<IClusteringService, CMedoidsClusteringService>();
    }
}