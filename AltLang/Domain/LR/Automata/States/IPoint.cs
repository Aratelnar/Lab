using System.Diagnostics.CodeAnalysis;
using AltLang.Domain.Grammar;
using Lang.Domain;
using Lang.GrammarTransform;

namespace Lang.Parser.LRAutomata;

public interface IPoint<TPoint> where TPoint : IPoint<TPoint>
{
    IRule Rule { get; }
    int Pointer { get; }

    public (IRule, int) Core => (Rule, Pointer);

    static abstract TPoint FromRule(IRule rule);
    static abstract bool TryCombine(TPoint a, TPoint b, [MaybeNullWhen(false)] out TPoint result);
    TPoint ShiftPoint();

    public Token GetCurrentToken() =>
        Pointer == Rule.Tokens.Count
            ? Terminal.End
            : Rule.Tokens[Pointer];

     public bool ShouldMakeClosure() => GetCurrentToken() is NonTerminal;

     public IEnumerable<TPoint> Closure(IGrammar grammar, FirstSet firstSet);
     string ToKey();
}