// using Lang.Domain;

using Lang.Util.RecordCollections;

namespace AltLang.Domain.Grammar;

public static class Grammar
{
    public static Grammar<TRule> Create<TRule>(NonTerminal axiom, RecordHashSet<TRule> rules) where TRule : IRule => new(axiom, rules);
}

public record Grammar<TRule>(NonTerminal Axiom, RecordHashSet<TRule> Rules_) : IGrammar where TRule : IRule
{
    public IEnumerable<IRule> Rules => Rules_.AsEnumerable().Cast<IRule>();
    public IEnumerable<NonTerminal> GetAllSources() => Rules.Select(r => r.Source).ToHashSet();

    public Grammar<TRule> Copy() =>
        this with {Rules_ = Rules_.ToHashSet()};

    public IEnumerable<TRule> GetRulesBySource_(NonTerminal source) => Rules_.Where(r => r.Source == source);
    public IEnumerable<IRule> GetRulesBySource(NonTerminal source) => Rules.Where(r => r.Source == source);
}

public interface IGrammar
{
    IEnumerable<NonTerminal> GetAllSources();
    IEnumerable<IRule> GetRulesBySource(NonTerminal source);
    IEnumerable<IRule> Rules { get; }
    NonTerminal Axiom { get; }
}