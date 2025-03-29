using System.Numerics;

namespace Lang.Domain;

public readonly record struct Priority(params short[] Items) : IComparable<Priority>, IComparisonOperators<Priority, Priority, bool>
{
    public Priority Extend(short next) => new(Items.Append(next).ToArray());
    public Priority Extend(Priority next) => new(Items.Concat(next.Items).ToArray());
    public bool Equals(Priority other) => Items.SequenceEqual(other.Items);

    public static Priority Min(Priority a, Priority b) => a < b ? a : b;
    public Priority Abs() => new(Items.Select(Math.Abs).ToArray());

    public override int GetHashCode() => Items.Aggregate(0, HashCode.Combine);
    public override string ToString() => string.Join('.', Items);

    public static Priority Default = new(0);
    public int CompareTo(Priority other)
    {
        var len = Math.Min(Items.Length, other.Items.Length);
        for (var i = 0; i < len; i++)
        {
            var x = Items[i];
            var y = other.Items[i];
            var result = CompareSegments(x,y);
            if(result != 0) return result;
        }
        return Items.Length.CompareTo(other.Items.Length);
    }

    private static int CompareSegments(short x, short y)
    {
        var xVal = Math.Abs(x);
        var yVal = Math.Abs(y);
        var abs = xVal.CompareTo(yVal);
        if(abs != 0) return abs;
        return -x.CompareTo(y);
    }

    public static bool operator >(Priority left, Priority right) => left.CompareTo(right) > 0;

    public static bool operator >=(Priority left, Priority right) => left.CompareTo(right) >= 0;

    public static bool operator <(Priority left, Priority right) => left.CompareTo(right) < 0;

    public static bool operator <=(Priority left, Priority right) => left.CompareTo(right) <= 0;
}