using System.Diagnostics.CodeAnalysis;
using AltLang.Domain.Grammar;
using Lang.Domain;

namespace Lang.Parser.LRAutomata;

public class LrAutomata<TState> where TState : ILrState<TState>
{
    public HashSet<TState> States { get; }
    public Dictionary<string, Dictionary<Token, TState>> Transitions { get; }

    public TState InitialState { get; private set; }

    public LrAutomata()
    {
        States = new HashSet<TState>();
        Transitions = new Dictionary<string, Dictionary<Token, TState>>();
    }

    public void SetInitialState(TState initialState)
    {
        InitialState = initialState;
        States.Add(initialState);
    }

    public bool AddTransition(TState state, Token token, TState nextState)
    {
        bool shouldUpdate;
        if (!Transitions.TryGetValue(state.ToString(), out var trans))
            trans = Transitions[state.ToString()] = new Dictionary<Token, TState>();
        if (trans.TryGetValue(token, out var actualState) || TryGetState(nextState, out actualState)) {
            shouldUpdate = actualState.MergeWith(nextState);
        } else {
            shouldUpdate = true;
            actualState = nextState;
            States.Add(actualState);
        }

        trans[token] = actualState;
        return shouldUpdate;
    }

    private bool TryGetState(TState state, [MaybeNullWhen(false)] out TState actualState)
    {
        actualState = States.FirstOrDefault(s => s.Equals(state));
        return actualState != null;
    }
}