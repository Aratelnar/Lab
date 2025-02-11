using Lang.Util.RecordCollections;

namespace AltLang.Domain.Grammar.Rules;

public record Rule(NonTerminal Source, RecordList<Token> Tokens) : IRule
{
    public virtual bool Equals(Rule? other) => ToKey() == other?.ToKey();

    public override int GetHashCode() => Tokens.Aggregate(Source.GetHashCode(), HashCode.Combine);

    public Rule Copy() => this with {Tokens = Tokens.ToList()};

    public Rule RemoveToken(Token token)
    {
        var newTokens = Tokens.ToList();
        newTokens.Remove(token);
        return this with {Tokens = newTokens};
    }

    public override string ToString() => $"{Source} -> {string.Join(" ", Tokens)}";
    public T Lift<T>() where T : IRule => this switch
    {
        T t => t,
        _ => throw new InvalidCastException()
    };

    public string ToKey() => ToString();
}