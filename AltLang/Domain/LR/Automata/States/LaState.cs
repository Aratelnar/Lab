using System.Collections.Immutable;
using System.Diagnostics;
using AltLang.Domain.Grammar;
using Lang.Domain;
using Lang.GrammarTransform;

namespace Lang.Parser.LRAutomata;

public record LaState<TPoint>(ImmutableHashSet<TPoint> Rules) : ILrState<LaState<TPoint>>
    where TPoint : IPoint<TPoint>
{
    public virtual bool Equals(LaState<TPoint>? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Rules.Select(r => r.Core).ToHashSet().SetEquals(other.Rules.Select(r => r.Core));
    }

    public override string ToString() =>
        string.Join("\n", Rules.OrderByDescending(r => r.Pointer).ThenBy(r => r.Rule.ToKey()).Select(r => r.ToKey()));

    public override int GetHashCode() => Rules.Aggregate(0, HashCode.Combine);

    public bool MergeWith(LaState<TPoint> other)
    {
        var changed = false;
        var coreToPoint = Rules.ToDictionary(r => r.Core, r => r);
        foreach (var rule in other.Rules)
        {
            var initial = coreToPoint[rule.Core];
            if(!TPoint.TryCombine(initial, rule, out var combined) || combined.Equals(initial)) continue;
            changed = true;
            coreToPoint[rule.Core] = combined;
        }
        if(changed) Rules = coreToPoint.Values.ToImmutableHashSet();

        return changed;
    }

    public static LaState<TPoint> GetInitialState(IGrammar g)
    {
        var set = g.GetRulesBySource(g.Axiom)
            .Select(TPoint.FromRule)
            .ToImmutableHashSet();
        return new LaState<TPoint>(Closure(set, g));
    }

    public LaState<TPoint> Goto(Token t, IGrammar g) => new(Goto(Rules, t, g));

    private static ImmutableHashSet<TPoint> Closure(ImmutableHashSet<TPoint> rules, IGrammar g)
    {
        var coreToPoint = rules.ToDictionary(r => r.Core, r => r);
        var queue = new Queue<TPoint>(rules);
        var firstSet = new FirstSet(g);
        while (queue.Count > 0)
        {
            var point = queue.Dequeue();
            if (!point.ShouldMakeClosure()) continue;
            var points = point.Closure(g, firstSet).ToList();
            foreach (var p in points)
            {
                if(!coreToPoint.TryGetValue(p.Core, out var comb))
                {
                    coreToPoint[p.Core] = p;
                    queue.Enqueue(p);
                }
                else if (TPoint.TryCombine(comb, p, out var combined))
                {
                    if(!combined.Equals(comb))
                        queue.Enqueue(combined);
                    coreToPoint[p.Core] = combined;
                }
            }
        }

        return coreToPoint.Values.ToImmutableHashSet();
    }

    private static ImmutableHashSet<TPoint> Goto(ImmutableHashSet<TPoint> rules, Token t, IGrammar g) =>
        Closure(rules
            .Where(r => r.GetCurrentToken() == t)
            .Select(p => p.ShiftPoint())
            .ToImmutableHashSet(), g);

    public IEnumerable<Token> GetTransitions() =>
        Rules.Select(r => r.GetCurrentToken())
            .Where(t => t != Terminal.End)
            .Distinct();

    public ImmutableHashSet<TPoint> Rules { get; private set; } = Rules;
}