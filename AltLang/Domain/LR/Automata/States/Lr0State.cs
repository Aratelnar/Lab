// using AltLang.Domain.Grammar;
// using Lang.Domain;
//
// namespace Lang.Parser.LRAutomata;
//
// public record Lr0State(HashSet<Point> Rules) : ILrState<Lr0State>
// {
//     public Lr0State? Append(Lr0State b) => null;
//
//     public static Lr0State GetInitialState<T>(Grammar<T> g) where T : IRule =>
//         new(Closure(new HashSet<Point>(
//             g.GetRulesBySource(g.Axiom).Select(r => Point.FromRule(r))), g));
//
//     public Lr0State Goto<T>(Token t, Grammar<T> g) where T : IRule => new(Goto(Rules, t, g));
//
//     private static HashSet<Point> Closure<T>(HashSet<Point> rules, Grammar<T> g) where T : IRule
//     {
//         var result = new HashSet<Point>(rules);
//         var queue = new Queue<Point>(rules);
//         while (queue.Count > 0)
//         {
//             var point = queue.Dequeue();
//             if (!((IPoint<Point>)point).ShouldMakeClosure()) continue;
//             var token = ((IPoint<Point>)point).GetCurrentToken() as NonTerminal;
//             var points = g.GetRulesBySource(token!).Select(r => Point.FromRule(r)).ToList();
//             foreach (var p in points.Except(result)) queue.Enqueue(p);
//             result.UnionWith(points);
//         }
//
//         return result;
//     }
//
//     private static HashSet<Point> Goto<T>(HashSet<Point> rules, Token t, Grammar<T> g) where T : IRule =>
//         Closure(rules
//             .Where(r => ((IPoint<Point>)r).GetCurrentToken() == t)
//             .Select(p => p.ShiftPoint())
//             .ToHashSet(), g);
//
//     public IEnumerable<Token> GetTransitions() =>
//         Rules.Select(r => ((IPoint<Point>)r).GetCurrentToken())
//             .Where(t => t != Terminal.End)
//             .Distinct();
//
//     public virtual bool Equals(Lr0State? other)
//     {
//         if (ReferenceEquals(null, other)) return false;
//         if (ReferenceEquals(this, other)) return true;
//         return Rules.SetEquals(other.Rules);
//     }
//
//     public override int GetHashCode() => Rules.Aggregate(0, HashCode.Combine);
// }