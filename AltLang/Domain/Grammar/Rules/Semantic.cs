using Lang.RuleReader.Semantic;
using Lang.Util.RecordCollections;

namespace AltLang.Domain.Grammar.Rules;

public record Semantic<TRule>(TRule Core, ObjectDefinition Reduce) : IRuleSemantic, IRule
    where TRule : IRule
{
    public virtual bool Equals(Semantic<TRule>? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Core.Equals(other.Core);
    }

    public override int GetHashCode() => Core.GetHashCode();
    public override string ToString() => $"({Core.ToString()}); {Reduce}";
    public NonTerminal Source => Core.Source;
    public RecordList<Token> Tokens => Core.Tokens;

    public T Lift<T>() where T : IRule => this switch
    {
        T t => t,
        _ => Core.Lift<T>()
    };

    public string ToKey() => Core.ToKey();
}

public interface IRuleSemantic : IRule
{
    ObjectDefinition Reduce { get; }
}