using Lang.Domain;
using Lang.Util.RecordCollections;

namespace AltLang.Domain.Grammar.Rules;

public record Prioritized<TRule>(TRule Core, Priority Priority) : IRulePrioritized, IRule
    where TRule : IRule
{
    public NonTerminal Source => Core.Source;
    public RecordList<Token> Tokens => Core.Tokens;

    public T Lift<T>() where T : IRule => this switch
    {
        T t => t,
        _ => Core.Lift<T>()
    };

    public string ToKey() => Core.ToKey();

    public virtual bool Equals(Semantic<TRule>? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Core.Equals(other.Core);
    }

    public override int GetHashCode() => Core.GetHashCode();
    public override string ToString() => $"({Core.ToString()}); {Priority}";
}

public interface IRulePrioritized : IRule
{
    Priority Priority { get; }
}