using Lang.Domain;

namespace Lang.Parser.LRAutomata;

public record Lr1Point(Point Core, HashSet<Terminal> Terminals)
{
    public virtual bool Equals(Lr1Point? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Core.Equals(other.Core) && Terminals.SetEquals(other.Terminals);
    }

    public override int GetHashCode() => Terminals.Aggregate(Core.GetHashCode(), HashCode.Combine);
    public override string ToString() => $"{Core}; {string.Join(",", Terminals)}";
};