using AltLang.Domain.Grammar;
using Lang.Domain;

namespace Lang.Parser.LRAutomata;

public interface ILrState<TState> where TState : ILrState<TState>
{
    bool MergeWith(TState other);
    // TState? Append(TState b);
    static abstract TState GetInitialState(IGrammar g);
    TState Goto(Token t, IGrammar g);
    IEnumerable<Token> GetTransitions();
}