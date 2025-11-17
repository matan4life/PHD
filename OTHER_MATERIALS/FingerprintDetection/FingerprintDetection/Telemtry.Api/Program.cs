using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(corsPolicyBuilder => corsPolicyBuilder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
);

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/sessions", () =>
    {
        const string telemetryPath = @"D:\PHD\FingerprintDetection\Telemetry";
        var sessions = Directory.GetDirectories(telemetryPath)
            .Select(x => new DirectoryInfo(x))
            .Select(x => new TelemetrySession(x.Name, x.CreationTime));
        return sessions;
    })
    .WithName("sessions")
    .WithOpenApi();

app.MapGet("/sessions/{sessionId}", (string sessionId) =>
    {
        const string telemetryPath = @"D:\PHD\FingerprintDetection\Telemetry";
        var sessionPath = Path.Combine(telemetryPath, sessionId);
        var comparisons = Directory.GetDirectories(sessionPath)
            .Select(x => new DirectoryInfo(x).Name)
            .Select(x => new ClustersComparisonTelemetry(int.Parse(x.Split("_")[0]), int.Parse(x.Split("_")[1])));
        return comparisons;
    })
    .WithName("session")
    .WithOpenApi();

app.MapGet("/sessions/{sessionId}/{firstCluster:int}/{secondCluster:int}",
        (string sessionId, int firstCluster, int secondCluster) =>
        {
            const string telemetryPath = @"D:\PHD\FingerprintDetection\Telemetry";
            var sessionPath = Path.Combine(telemetryPath, sessionId, $"{firstCluster}_{secondCluster}");
            var comparisons = Directory.GetFiles(sessionPath)
                .Select(Path.GetFileNameWithoutExtension)
                .Select(x => new ClusterPositionTelemetry(int.Parse(x.Split("_")[0]), int.Parse(x.Split("_")[1])));
            return comparisons;
        })
    .WithName("rotations")
    .WithOpenApi();

app.MapGet("/sessions/{sessionId}/{firstCluster:int}/{secondCluster:int}/{firstPosition:int}/{secondPosition:int}",
        (string sessionId, int firstCluster, int secondCluster, int firstPosition, int secondPosition) =>
        {
            var telemetryPath =
                @$"D:\PHD\FingerprintDetection\Telemetry\{sessionId}\{firstCluster}_{secondCluster}\{firstPosition}_{secondPosition}.json";
            var telemetry = File.ReadAllText(telemetryPath);
            return telemetry;
        })
    .WithName("comparison")
    .WithOpenApi();

app.MapGet("/descriptors/{sessionId}/{cluster:int}/{prefix:int}",
        (string sessionId, int cluster, int prefix) =>
        {
            var telemetryPath =
                @$"D:\PHD\FingerprintDetection\Telemetry\{sessionId}\{prefix}_{cluster}.json";
            var telemetry = File.ReadAllText(telemetryPath);
            return telemetry;
        })
    .WithName("descriptor")
    .WithOpenApi();

app.MapPost("/dataset", ([FromBody] string path) =>
{
    var availableFiles = Directory.GetFiles(path);
    var processedPairs = new Dictionary<string, List<string>>();
    foreach (var file in availableFiles)
    {
        foreach (var otherFile in availableFiles.Where(f => f != file))
        {
            processedPairs.TryGetValue(file, out var processedFiles);
            processedPairs.TryGetValue(otherFile, out var otherProcessedFiles);
            if ((processedFiles?.Contains(otherFile) ?? false) || (otherProcessedFiles?.Contains(file) ?? false))
            {
                continue;
            }
            
            var p = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName =
                        @"D:\PHD\\FingerprintDetection\FingerprintDetection\Presentation.Console\bin\Debug\net8.0\Presentation.Console.exe",
                    Arguments = $"{file} {otherFile}"
                }
            };
            p.Start();
            p.WaitForExit();
            
            if (processedPairs.TryGetValue(file, out var value))
            {
                value.Add(otherFile);
            }
            else
            {
                processedPairs.Add(file, [otherFile]);
            }
        }
    }
});

app.Run();

public sealed record TelemetrySession(string SessionId, DateTime SessionCreated);

public sealed record ClustersComparisonTelemetry(int FirstClusterId, int SecondClusterId);

public sealed record ClusterPositionTelemetry(int FirstPosition, int SecondPosition);