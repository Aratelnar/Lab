namespace Lang.Util;

public static class LinqExtensions
{
    public static IEnumerable<T> At<T>(this IList<T> list, IEnumerable<int> indices) =>
        indices.Select(index => list[index]);

    public static Dictionary<TKey, S> Map<TKey, T, S>(this Dictionary<TKey, T> dict, Func<T, S> selector)
        where TKey : notnull =>
        dict.ToDictionary(p => p.Key, p => selector(p.Value));

    public static Dictionary<TKey, T> ToMap<TKey, T>(this IEnumerable<(TKey, T)> dict)
        where TKey : notnull =>
        dict.ToDictionary(p => p.Item1, p => p.Item2);

    public static bool EqualsKeys<TKey, T>(this Dictionary<TKey, T> dict, Dictionary<TKey, T> other)
        where TKey : notnull
    {
        var keys = dict.Keys.ToHashSet();
        keys.SymmetricExceptWith(other.Keys);
        return keys.Count == 0;
    }

    public static IEnumerable<(TKey, R)> Intersect<TKey, T, R>(this Dictionary<TKey, T> dict, Dictionary<TKey, T> other, Func<T, T, R> map)
        where TKey : notnull
    {
        foreach (var (key,val) in dict)
        {
            if(!other.TryGetValue(key, out var r)) continue;
            yield return (key,map(val, r));
        }
    }

    public static Dictionary<K, V> Combine<K, V>(Dictionary<K, V> s1, Dictionary<K, V> s2)
        where K : notnull => s1.Over(s2, (a, _) => a);

    public static Dictionary<K, V> Over<K, V>(this Dictionary<K, V> over, Dictionary<K, V> under,
        Func<V, V, V> reduce)
    {
        var result = new Dictionary<K, V>();
        foreach (var key in over.Keys.Union(under.Keys))
        {
            var vUnder = under.GetValueOrDefault(key);
            var vOver = over.GetValueOrDefault(key);
            result[key] = (vUnder, vOver) switch
            {
                (null, null) => throw new KeyNotFoundException($"Key {key} not found"),
                (null, var o) => o,
                (var u, null) => u,
                var (u, o) => reduce(o, u),
            };
        }
        return result;
    }
}