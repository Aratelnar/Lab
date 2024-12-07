namespace Lang.Domain.Semantic;

public record Node(string Name, List<Node> Arguments)
{
    public override string ToString() => $"{Name} ({string.Join(",", Arguments.Select(i => i.ToString()))})";
}