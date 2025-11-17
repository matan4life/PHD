using System.Collections;

namespace Presentation.Console.Models;

public sealed class ShiftedIndexSequence(int range) : IEnumerable<int>
{
    private IEnumerable<int> Indices { get; set; } = Enumerable.Range(0, range);

    public IEnumerator<int> GetEnumerator()
    {
        return Indices.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void ShiftRight()
    {
        var firstValue = Indices.First();
        IEnumerable<int> shiftedCollection = [..Indices.Skip(1)];
        Indices = shiftedCollection.Append(firstValue);
    }
}