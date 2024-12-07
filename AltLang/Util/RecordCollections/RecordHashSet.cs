
using System.Collections;

namespace Lang.Util.RecordCollections;

public record RecordHashSet<T>(HashSet<T> Values) : IEnumerable<T>
{
    public virtual bool Equals(RecordList<T>? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Values.SetEquals(other.Values);
    }
    
    public int Count => Values.Count;
    
    public IEnumerator<T> GetEnumerator() => Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override int GetHashCode() => Values.Aggregate(0, HashCode.Combine);

    public static implicit operator RecordHashSet<T>(HashSet<T> val) => new(val);
    public static implicit operator HashSet<T>(RecordHashSet<T> val) => val.Values;
}