using Api.Database;
using Api.Options;
using Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureApplication(this IServiceCollection serviceCollection)
    {
        serviceCollection.Configure<EnvironmentVariableOptions>(options =>
        {
            options.FlaskInputFolder = Environment.GetEnvironmentVariable("FLASK_INPUT") ?? "/flask_input";
            options.FlaskEnhancedOutputFolder = Environment.GetEnvironmentVariable("FLASK_ENHANCED_OUTPUT") ?? "/flask_enhanced_output";
            options.FlaskSkeletonOutputFolder = Environment.GetEnvironmentVariable("FLASK_SKELETON_OUTPUT") ?? "/flask_skeleton_output";
        });
        
        return serviceCollection
            .AddHttpClient()
            .AddTransient<IFileService, FileService>()
            .AddDbContext<FingerprintContext>(options =>
            options.UseSqlServer(
                "Server=localhost;Database=Fingerprint;Trusted_Connection=True;Integrated Security=True;" +
                "MultipleActiveResultSets=true;TrustServerCertificate=True;Connect Timeout=0"));
    }
}