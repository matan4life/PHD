namespace Presentation.Console.Models;

public sealed record Telemetry(bool IsSuccessful,
    int? FirstIndex,
    int? SecondIndex,
    string ValueType,
    double? FirstValue,
    double? SecondValue,
    int CurrentScore,
    int PreviousMaxScore,
    string StatusMessage);