using Lang.Domain;

namespace Lang.Parser.LRAutomata;

public record Lr0State(HashSet<Point> Rules) : ILrState<Lr0State>
{
    public bool MergeWith(Lr0State b) => false;

    public static Lr0State GetInitialState(Grammar g) =>
        new(Closure(new HashSet<Point>(
            g.GetRulesBySource(g.Axiom).Select(Point.FromRule)), g));

    public Lr0State Goto(Token t, Grammar g) => new(Goto(Rules, t, g));

    private static HashSet<Point> Closure(HashSet<Point> rules, Grammar g)
    {
        var result = new HashSet<Point>(rules);
        var queue = new Queue<Point>(rules);
        while (queue.Count > 0)
        {
            var point = queue.Dequeue();
            if(!point.ShouldMakeClosure()) continue;
            var token = point.GetCurrentToken() as NonTerminal;
            var points = g.GetRulesBySource(token!).Select(Point.FromRule).ToList();
            foreach (var p in points.Except(result)) queue.Enqueue(p);
            result.UnionWith(points);
        }

        return result;
    }

    private static HashSet<Point> Goto(HashSet<Point> rules, Token t, Grammar g) => 
        Closure(rules
            .Where(r => r.GetCurrentToken() == t)
            .Select(p => p.ShiftPoint())
            .ToHashSet(), g);

    public IEnumerable<Token> GetTransitions() =>
        Rules.Select(r => r.GetCurrentToken())
            .Where(t => t != Terminal.End)
            .Distinct();

    public virtual bool Equals(Lr0State? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Rules.SetEquals(other.Rules);
    }

    public override int GetHashCode() => Rules.Aggregate(0, HashCode.Combine);
}