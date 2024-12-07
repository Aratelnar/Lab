using Lang.Util.RecordCollections;

namespace Lang.Domain;

public record Grammar(NonTerminal Axiom, RecordHashSet<Rule> Rules)
{
    public IEnumerable<NonTerminal> GetAllSources() => Rules.Select(r => r.Source).ToHashSet();

    public Grammar Copy() =>
        this with {Rules = Rules.ToHashSet()};

    public IEnumerable<Rule> GetRulesBySource(NonTerminal source) => Rules.Where(r => r.Source == source);
}