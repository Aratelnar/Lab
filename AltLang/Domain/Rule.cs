using Lang.Util.RecordCollections;

namespace Lang.Domain;

public record Rule(NonTerminal Source, RecordList<Token> Tokens, int Priority = 0)
{
    public virtual bool Equals(Rule? other) => ToString() == other?.ToString();

    public override int GetHashCode() => Tokens.Aggregate(Source.GetHashCode(), HashCode.Combine);

    public Rule Copy() => new(Source, Tokens.ToList());

    public Rule RemoveToken(Token token)
    {
        var newTokens = Tokens.ToList();
        newTokens.Remove(token);
        return this with {Tokens = newTokens};
    }

    public override string ToString() => $"{Source} -> {string.Join(" ", Tokens)}";
}