using Lang.Domain;

namespace Lang.Parser.LRAutomata;

public class LrAutomata<TState> where TState : ILrState<TState>
{
    public HashSet<TState> States { get; }
    public Dictionary<TState, Dictionary<Token, TState>> Transitions { get; }

    public TState InitialState { get; private set; }

    public LrAutomata() {
        States = new HashSet<TState>();
        Transitions = new Dictionary<TState, Dictionary<Token, TState>>();
    }

    public void SetInitialState(TState initialState)
    {
        InitialState = initialState;
        States.Add(initialState);
    }

    public bool AddTransition(TState state, Token token, TState nextState)
    {
        bool shouldUpdate;
        if (!Transitions.TryGetValue(state, out var trans))
            trans = Transitions[state] = new Dictionary<Token, TState>();
        if (trans.TryGetValue(token, out var actualState) || States.TryGetValue(nextState, out actualState)) { 
            shouldUpdate = actualState.MergeWith(nextState);
        } else {
            actualState = nextState;
            shouldUpdate = true;
            States.Add(actualState);
        }

        trans[token] = actualState;
        return shouldUpdate;
    }
}