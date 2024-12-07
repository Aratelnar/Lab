using Lang.Domain;
using Lang.Domain.Semantic;
using Lang.GrammarTransform;
using Lang.Parser.LRAutomata;
using Lang.Parser.Semantic;

namespace Lang.Parser;

public class AutomataBuilder
{
    public static SemanticAutomata EmptyAutomata = new() {Axiom = new NonTerminal("Expr")};

    public Automata FromGrammar(Grammar grammar)
    {
        var automata = new Automata();
        var rules = grammar.Rules.ToDictionary(r => r.ToString());

        automata.Rules = rules;

        var lr = BuildLRAutomata<Lr0State>(grammar);
        var follow = new FollowSet(grammar);
        var counter = 0;
        var stateToId = new Dictionary<Lr0State, int>
        {
            [lr.InitialState] = 0
        };
        var queue = new Queue<(int, Lr0State)>();
        queue.Enqueue((0, lr.InitialState));

        while (queue.Count > 0)
        {
            var (q, state) = queue.Dequeue();
            foreach (var point in state.Rules.OrderByDescending(r => r.Rule.Priority))
                if (point.Pointer == point.Rule.Tokens.Count)
                {
                    foreach (var terminal in follow.GetFollowSet(point.Rule.Source))
                        automata.Actions[(q, terminal)] = new Automata.Reduce(point.Rule.ToString());
                }
                else
                {
                    var token = point.Rule.Tokens[point.Pointer];
                    var newState = lr.Transitions[state][token];
                    if (!stateToId.TryGetValue(newState, out var nq))
                    {
                        nq = ++counter;
                        stateToId[newState] = nq;
                        queue.Enqueue((nq, newState));
                    }

                    automata.Actions[(q, token)] = new Automata.Shift(nq);
                }
        }

        int end;
        if (!lr.Transitions[lr.InitialState].TryGetValue(grammar.Axiom, out var e) || (end = stateToId[e]) < 0)
        {
            automata.Actions[(0, grammar.Axiom)] = new Automata.Shift(++counter);
            end = counter;
        }

        automata.Actions[(end, Terminal.End)] = new Automata.Accept();

        return automata;
    }

    public static SemanticAutomata FromSemanticGrammar(SemanticGrammar grammar)
    {
        var automata = new SemanticAutomata
        {
            Axiom = grammar.Axiom
        };
        var rules = grammar.Rules
            .Select(r => new SemanticRuleShort(r.Core.Source, (byte) r.Core.Tokens.Count, r.Reduce))
            .ToList();
        var ruleNameToIndex = grammar.Rules
            .Select((r, i) => (r, i))
            .ToDictionary(r => r.r.ToString(), r => r.i);

        automata.Rules = rules;
        automata.KnownTokens = grammar.Rules
            .SelectMany(r => r.Core.Tokens)
            .ToHashSet();

        var coreGrammar = (Grammar) grammar;
        var lr = BuildLRAutomata<LaLr1State>(coreGrammar);
        var counter = 0;
        var stateToId = new Dictionary<LaLr1State, int>
        {
            [lr.InitialState] = 0
        };
        var queue = new Queue<(int, LaLr1State)>();
        queue.Enqueue((0, lr.InitialState));

        while (queue.Count > 0)
        {
            var (q, state) = queue.Dequeue();
            foreach (var point in state.Rules.OrderByDescending(r => r.Core.Rule.Priority))
                if (point.Core.Pointer == point.Core.Rule.Tokens.Count)
                {
                    foreach (var terminal in point.Terminals)
                        automata.Actions[(q, terminal)] =
                            new SemanticAutomata.Reduce(ruleNameToIndex[point.Core.Rule.ToString()],
                                point.Core.Rule.Priority);
                }
                else
                {
                    var token = point.Core.Rule.Tokens[point.Core.Pointer];
                    var newState = lr.Transitions[state][token];
                    if (!stateToId.TryGetValue(newState, out var nq))
                    {
                        nq = ++counter;
                        stateToId[newState] = nq;
                        queue.Enqueue((nq, newState));
                    }

                    automata.Actions[(q, token)] = new SemanticAutomata.Shift(nq, point.Core.Rule.Priority);
                }
        }

        int end;
        if (!lr.Transitions[lr.InitialState].TryGetValue(grammar.Axiom, out var e) || (end = stateToId[e]) < 0)
        {
            automata.Actions[(0, grammar.Axiom)] = new SemanticAutomata.Shift(++counter, 0);
            end = counter;
        }

        automata.Actions[(end, Terminal.End)] = new SemanticAutomata.Accept();

        return automata;
    }

    public static LrAutomata<TState> BuildLRAutomata<TState>(Grammar grammar) where TState : ILrState<TState>
    {
        var automata = new LrAutomata<TState>();
        var initialState = TState.GetInitialState(grammar);
        automata.SetInitialState(initialState);
        var queue = new Queue<TState>();
        queue.Enqueue(initialState);

        while (queue.Count > 0)
        {
            var state = queue.Dequeue();
            foreach (var token in state.GetTransitions())
            {
                var nextState = state.Goto(token, grammar);
                if (automata.AddTransition(state, token, nextState))
                    queue.Enqueue(nextState);
            }
        }

        return automata;
    }

    public static SemanticAutomata MergeAutomata(params SemanticAutomata[] automataList)
    {
        if (automataList.Length == 1) return automataList[0];
        var result = new SemanticAutomata {Axiom = automataList.First().Axiom};
        var tokens = new HashSet<Token> {Terminal.End};
        foreach (var automata in automataList)
            tokens.UnionWith(automata.KnownTokens);
        result.KnownTokens = tokens;
        result.Rules = new List<SemanticRuleShort>();
        var startIndex = new int[automataList.Length];
        var counter = 0;
        for (var i = 0; i < automataList.Length; i++)
        {
            startIndex[i] = counter;
            result.Rules.AddRange(automataList[i].Rules);
            counter += automataList[i].Rules.Count;
        }

        result.Rules = automataList.SelectMany(a => a.Rules).ToList();
        var initialStates = automataList.Select((a, i) => (au: i, st: 0)).ToHashSet();
        var queue = new Queue<HashSet<(int, int)>>();
        queue.Enqueue(initialStates);
        var stateCount = 1;
        var visited = new Dictionary<HashSet<(int, int)>, int>(new StateComparer())
        {
            [initialStates] = 0
        };
        while (queue.Count > 0)
        {
            var state = queue.Dequeue();
            foreach (var token in tokens)
            {
                var action = GetAction(state, token);
                if (action is not null) result.Actions[(visited[state], token)] = action;
            }
        }

        SemanticAutomata.Action? GetAction(HashSet<(int au, int st)> state, Token token)
        {
            IEnumerable<(int, SemanticAutomata.Action)> SelectState((int au, int st) p)
            {
                return automataList[p.au].Actions.TryGetValue((p.st, token), out var act) ||
                       (!automataList[p.au].KnownTokens.Contains(token) &&
                        automataList[p.au].Actions.TryGetValue((p.st, Terminal.End), out act))
                    ? [(p.au, act)]
                    : [];
            }


            var actions = state
                .SelectMany(SelectState)
                .Select(p => (p.Item1, p.Item2 switch
                {
                    SemanticAutomata.Reduce r => r with {Rule = startIndex[p.Item1] + r.Rule},
                    _ => p.Item2
                }))
                .ToList();
            if (actions.Count == 0) return null;
            if (actions.All(a => a.Item2 is SemanticAutomata.Accept) && actions.Count == state.Count)
                return new SemanticAutomata.Accept();
            var reduce = actions.OrderBy(a => a.Item2.Priority)
                .FirstOrDefault(a => a.Item2 is SemanticAutomata.Reduce);
            var shift = actions.Where(a => a.Item2.Priority < (reduce.Item2?.Priority ?? 256))
                .Where(a => a.Item2 is SemanticAutomata.Shift)
                .Select(a => (a.Item1, a.Item2 as SemanticAutomata.Shift));

            var newState = new HashSet<(int, int)>();

            var priority = reduce.Item2?.Priority ?? 256;
            foreach (var (au, s) in shift)
            {
                var aut = automataList[au];
                if (aut.ShouldClosure(s!.NextState))
                    newState.UnionWith(initialStates);
                newState.Add((au, s.NextState));
                priority = Math.Min(priority, s.Priority);
            }

            if (newState.Count == 0) return reduce.Item2!;
            if (!visited.TryGetValue(newState, out var id))
            {
                id = visited[newState] = stateCount++;
                queue.Enqueue(newState);
            }

            return new SemanticAutomata.Shift(id, priority);
        }

        if (!result.Actions.TryGetValue((0, result.Axiom), out var e))
            e = result.Actions[(0, result.Axiom)] = new SemanticAutomata.Shift(stateCount, 0);
        if (e is not SemanticAutomata.Shift {NextState: var end})
            throw new Exception();

        result.Actions[(end, Terminal.End)] = new SemanticAutomata.Accept();

        return result;
    }

    private class StateComparer : IEqualityComparer<HashSet<(int, int)>>
    {
        public bool Equals(HashSet<(int, int)>? x, HashSet<(int, int)>? y)
        {
            return x!.SetEquals(y!);
        }

        public int GetHashCode(HashSet<(int, int)> obj)
        {
            return obj.Aggregate(0, HashCode.Combine);
        }
    }
}