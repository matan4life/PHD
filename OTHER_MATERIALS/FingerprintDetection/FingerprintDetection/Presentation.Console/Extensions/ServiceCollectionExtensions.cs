using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Presentation.Console.EventSourcing;
using Presentation.Console.Features.GetClusterDescriptor;
using Presentation.Console.Features.GetMinutiae;
using Presentation.Console.Features.SeparateByClusters;
using Presentation.Console.Models;
using Presentation.Console.Services;

namespace Presentation.Console.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        var options = Options.Create(new PythonExecutionOptions(
            "powershell",
            @"D:\PHD\Fingerprint",
            ".venv/Scripts/activate.ps1",
            "main.py"
        ));
        
        return services.AddLogging(b =>
            {
                b.AddConsole();
                b.SetMinimumLevel(LogLevel.Debug);
            })
            .AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(typeof(Program).Assembly)
                    .AddRequestPostProcessor<
                        ResultPostLogger<GetMinutiaeQuery, GetMinutiaeResponse, GetMinutiaeErrors>>()
                    .AddRequestPostProcessor<PostLogger<SeparateByClustersCommand, SeparateByClustersResponse>>()
                    .AddRequestPostProcessor<PostLogger<GetClusterDescriptorQuery, GetClusterDescriptorResponse>>();
            })
            .AddSingleton<IMapper<string, IEnumerable<Minutia>>, JsonStringToGenericMapper<IEnumerable<Minutia>>>()
            .AddSingleton<IOptions<PythonExecutionOptions>>(_ => options)
            .AddSingleton<IDatabase, Database>()
            .AddSingleton<IEventStore, EventStore>()
            .AddSingleton<IExecutable<IEnumerable<Minutia>>, PythonExecutor<IEnumerable<Minutia>>>();
    }
}