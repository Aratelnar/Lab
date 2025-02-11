using System.Text;

namespace LabEntry.domain;

public abstract record SemanticObject;

public record Structure(string Name, Dictionary<string, SemanticObject> Children) : SemanticObject
{
    public SemanticObject this[string key] => Children[key];

    public override string ToString() =>
        $"{Name}{{{string.Join(",", Children.Select(kv => $"{kv.Key}: {kv.Value}"))}}}";
};

public record Word(string Name) : SemanticObject
{
    public override string ToString() => Name;
}

public abstract record TypeObject
{
    public static TypeObject FromSemanticObject(SemanticObject obj) => obj switch
    {
        Structure {Name: "Template", Children: var c} => c["pattern"] switch
        {
            Structure s => new Template(s.Name,
                s.Children.ToDictionary(p => p.Key, p => FromSemanticObject(p.Value))),
            Word w => new Var(w.Name),
        },
        Structure {Name: "And", Children: var c} => new And(FromSemanticObject(c["left"]),
            FromSemanticObject(c["right"])),
        Structure {Name: "Or", Children: var c} => new Or(FromSemanticObject(c["left"]),
            FromSemanticObject(c["right"])),
        _ => throw new NotImplementedException()
    };
};

public record Template(string Name, Dictionary<string, TypeObject> Children) : TypeObject;

public record And(TypeObject Left, TypeObject Right) : TypeObject;

public record Or(TypeObject Left, TypeObject Right) : TypeObject;

public record Var(string Name) : TypeObject;