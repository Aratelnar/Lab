namespace Lang.Domain.Semantic;

public record SemanticGrammar(NonTerminal Axiom, HashSet<SemanticRule> Rules)
{
    public virtual bool Equals(SemanticGrammar? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        if (!Axiom.Equals(other.Axiom) || !Rules.Count.Equals(other.Rules.Count)) return false;
        return Rules.SetEquals(other.Rules);
    }

    public override int GetHashCode() => Rules.Aggregate(Axiom.GetHashCode(), HashCode.Combine);

    public static explicit operator Grammar(SemanticGrammar semanticGrammar) =>
        new(semanticGrammar.Axiom, semanticGrammar.Rules.Select(r => r.Core).ToHashSet());
};