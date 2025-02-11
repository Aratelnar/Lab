using System.Collections;
using System.Diagnostics.CodeAnalysis;
using LabEntry.domain;

namespace AltLang.Template;

public class TemplateDictionary<T> : IReadOnlyDictionary<SemanticObject, T>
{
    private enum NameType
    {
        Structure,
        Word
    }

    private record Name(string Value, NameType Type);

    private record Key(int State, string Property, Name? Name);

    private Dictionary<Key, int> _treeMap = new();
    private Dictionary<string, T> _setMap = new();
    private int _stateCount = 1;

    public bool ContainsKey(SemanticObject key) => throw new NotImplementedException();

    public T this[SemanticObject key] => this.GetValueOrDefault(key) ?? throw new KeyNotFoundException();
    public IEnumerable<SemanticObject> Keys { get; }
    public IEnumerable<T> Values { get; }

    public bool TryGetValue(SemanticObject obj, [MaybeNullWhen(false)] out T value)
    {
        value = default;
        var set = new HashSet<int>();
        var queue = new Queue<(int, string, SemanticObject)>();
        queue.Enqueue((0, "", obj));
        while (queue.Count > 0)
        {
            var (state, prop, val) = queue.Dequeue();
            if (!_treeMap.TryGetValue(new Key(state, prop, GetName(val)), out var next) &&
                !_treeMap.TryGetValue(new Key(state, prop, null), out next)) continue;
            set.Add(next);
            if (val is not Structure {Children: var c}) continue;
            foreach (var (p, v) in c) queue.Enqueue((next, p, v));
        }

        return _setMap.TryGetValue(string.Join(",", set.Order()), out value);

        Name GetName(SemanticObject obj) => obj switch
        {
            Word w => new Name(w.Name, NameType.Word),
            Structure s => new Name(s.Name, NameType.Structure),
        };
    }

    public void RegisterWord(Word w, T value)
    {
        var key = new Key(0, "", new Name(w.Name, NameType.Word));
        if (!_treeMap.TryGetValue(key, out var next))
            next = _treeMap[key] = _stateCount++;
        _setMap[next.ToString()] = value;
    }
    public void Register(TypeObject type, T value)
    {
        var set = new HashSet<int>();
        var queue = new Queue<(int, string, TypeObject)>();
        queue.Enqueue((0, "", type));
        while (queue.Count > 0)
        {
            var (state, prop, t) = queue.Dequeue();
            if (t is not (LabEntry.domain.Template or Var))
                continue;

            var name = (t as LabEntry.domain.Template)?.Name;
            var key = new Key(state, prop, name != null
                ? new Name(name, NameType.Structure)
                : null);
            if (!_treeMap.TryGetValue(key, out var next))
                next = _treeMap[key] = _stateCount++;
            if (t is LabEntry.domain.Template temp)
                foreach (var (k, v) in temp.Children)
                    queue.Enqueue((next, k, v));
            set.Add(next);
        }

        _setMap[string.Join(",", set.Order())] = value;
    }

    public IEnumerator<KeyValuePair<SemanticObject, T>> GetEnumerator() => throw new NotImplementedException();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count { get; }
}