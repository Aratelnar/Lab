using System.Collections;
using System.Collections.Immutable;

namespace Lang.Util.RecordCollections;

public record RecordList<T>(T[] Values) : IEnumerable<T>
{
    public virtual bool Equals(RecordList<T>? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Values.Length != other.Values.Length) return false;
        return !Values.Where((t, i) => !t.Equals(other.Values[i])).Any();
    }

    public int Count => Values.Length;
    public T this[int index] => Values[index];
    public IEnumerable<T> this[Range range] => Values[range];

    public IEnumerator<T> GetEnumerator() => Values.Select(x=>x).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override int GetHashCode() => Values.Aggregate(0, HashCode.Combine);

    public static implicit operator RecordList<T>(List<T> val) => new(val.ToArray());
    public static implicit operator List<T>(RecordList<T> val) => val.Values.ToList();
}