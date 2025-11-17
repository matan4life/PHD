namespace Api.Extensions;

public static class EnumerableExtensions
{
    public static async Task ParallelForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action)
    {
        var tasks = source.Select(item => Task.Run(async () => await action(item)));
        await Task.WhenAll(tasks);
    }
    
    public static async Task<IEnumerable<U>> ParallelSelectAsync<T, U>(this IEnumerable<T> source, Func<T, Task<U>> selector)
    {
        var tasks = source.Select(item => Task.Run(async () => await selector(item)));
        return await Task.WhenAll(tasks);
    }
}