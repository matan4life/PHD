using System.Text.Json;

namespace Presentation.Console.Services;

public interface IMapper<in TFrom, out TTo>
{
    TTo Map(TFrom from);
}

public sealed class JsonStringToGenericMapper<TResult> : IMapper<string, TResult>
{
    private JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TResult Map(string from)
    {
        return JsonSerializer.Deserialize<TResult>(from, JsonSerializerOptions) ?? throw new InvalidOperationException();
    }
}