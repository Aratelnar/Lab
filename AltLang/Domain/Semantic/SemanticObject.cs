using System.Text;

namespace LabEntry.domain;

public abstract record SemanticObject;

public record Structure(string Name, Dictionary<string, SemanticObject> Children) : SemanticObject
{
    public SemanticObject this[string key] => Children[key];

    public override string ToString() => $"{Name}{{{string.Join(",", Children.Select(kv => $"{kv.Key}: {kv.Value}"))}}}";
};

public record Word(string Name) : SemanticObject
{
    public override string ToString() => Name;
}