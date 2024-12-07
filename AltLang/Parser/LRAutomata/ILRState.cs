using Lang.Domain;

namespace Lang.Parser.LRAutomata;

public interface ILrState<TState> where TState : ILrState<TState>
{
    bool MergeWith(TState b);
    static abstract TState GetInitialState(Grammar g);
    TState Goto(Token t, Grammar g);
    IEnumerable<Token> GetTransitions();
}