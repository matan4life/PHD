namespace Demo;

public static class Extensions
{
    public static IGrouping<TKey, TElement> AsGrouping<TKey, TElement>(this IEnumerable<TElement> source, TKey key)
    {
        return new Grouping<TKey, TElement>(key, source);
    }
}

public sealed record Grouping<TKey, TElement>(TKey Key, IEnumerable<TElement> Elements) : IGrouping<TKey, TElement>
{
    public TKey Key { get; } = Key;

    public IEnumerator<TElement> GetEnumerator() => Elements.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
