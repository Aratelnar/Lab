using LabEntry.domain;
using Lang.RuleReader.Semantic;

namespace Lang.Domain.Semantic;

public record SemanticRule(Rule Core, SemanticObjectDefinition Reduce)
{
    public virtual bool Equals(SemanticRule? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Core.Equals(other.Core);
    }

    public override int GetHashCode() => Core.GetHashCode();
    public override string ToString() => Core.ToString();
}