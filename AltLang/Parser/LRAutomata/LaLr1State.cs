using System.Diagnostics;
using Lang.Domain;
using Lang.GrammarTransform;

namespace Lang.Parser.LRAutomata;

public record LaLr1State(HashSet<Lr1Point> Rules) : ILrState<LaLr1State>
{
    public virtual bool Equals(LaLr1State? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Rules.Select(r => r.Core).ToHashSet().SetEquals(other.Rules.Select(r => r.Core));
    }

    public override int GetHashCode() => Rules.Aggregate(0, (acc, r) => HashCode.Combine(acc, r.Core.GetHashCode()));

    public bool MergeWith(LaLr1State b)
    {
        var changed = false;
        var pointToLr1 = Rules.ToDictionary(r => r.Core, r => r);
        foreach (var rule in b.Rules)
        {
            changed |= !pointToLr1[rule.Core].Terminals.IsSupersetOf(rule.Terminals);
            pointToLr1[rule.Core].Terminals.UnionWith(rule.Terminals);
        }

        return changed;
    }

    public static LaLr1State GetInitialState(Grammar g)
    {
        var set = g.GetRulesBySource(g.Axiom)
            .Select(r => new Lr1Point(Point.FromRule(r), new HashSet<Terminal> {Terminal.End}))
            .ToHashSet();
        return new LaLr1State(Closure(set, g));
    }

    public LaLr1State Goto(Token t, Grammar g) => new(Goto(Rules, t, g));

    private static HashSet<Lr1Point> Closure(HashSet<Lr1Point> rules, Grammar g)
    {
        var result = new HashSet<Lr1Point>(rules);
        var queue = new Queue<Lr1Point>(rules);
        var firstSet = new FirstSet(g);
        while (queue.Count > 0)
        {
            var point = queue.Dequeue();
            if (!point.Core.ShouldMakeClosure()) continue;
            var lookahead = GetLookaheadTerminals(point.Core.GetLookaheadTokens(), point.Terminals, firstSet);
            var token = point.Core.GetCurrentToken() as NonTerminal;
            var points = g.GetRulesBySource(token!).Select(rule => new Lr1Point(Point.FromRule(rule), lookahead)).ToList();
            foreach (var p in points.Except(result)) queue.Enqueue(p);
            result.UnionWith(points);
        }

        return Merge(result);
    }

    private static HashSet<Lr1Point> Goto(HashSet<Lr1Point> rules, Token t, Grammar g) =>
        Closure(rules
            .Where(r => r.Core.GetCurrentToken() == t)
            .Select(p => p with {Core = p.Core.ShiftPoint()})
            .ToHashSet(), g);

    private static HashSet<Terminal> GetLookaheadTerminals(IEnumerable<Token> tokens, HashSet<Terminal> terminals,
        FirstSet firstSet)
    {
        var result = firstSet.GetFirstSet(tokens);
        if (!result.Contains(Terminal.Lambda)) return result;
        result.Remove(Terminal.Lambda);
        result.UnionWith(terminals);
        return result;
    }

    private static HashSet<Lr1Point> Merge(HashSet<Lr1Point> points)
    {
        var pointToTerminals = new Dictionary<Point, HashSet<Terminal>>();
        foreach (var point in points)
        {
            if (!pointToTerminals.ContainsKey(point.Core)) pointToTerminals[point.Core] = new HashSet<Terminal>();
            pointToTerminals[point.Core].UnionWith(point.Terminals);
        }

        return pointToTerminals.Select(p => new Lr1Point(p.Key, p.Value)).ToHashSet();
    }

    public IEnumerable<Token> GetTransitions() =>
        Rules.Select(r => r.Core.GetCurrentToken())
            .Where(t => t != Terminal.End)
            .Distinct();
}