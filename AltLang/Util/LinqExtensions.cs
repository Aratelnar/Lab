namespace Lang.Util;

public static class LinqExtensions
{
    public static IEnumerable<T> At<T>(this IList<T> list, IEnumerable<int> indices) => indices.Select(index => list[index]);
}