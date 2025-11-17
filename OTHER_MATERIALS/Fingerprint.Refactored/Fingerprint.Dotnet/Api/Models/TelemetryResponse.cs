namespace Api.Models;

public sealed record TelemetryResponse(DateTime Start, DateTime End, TimeSpan ExecutionTime);