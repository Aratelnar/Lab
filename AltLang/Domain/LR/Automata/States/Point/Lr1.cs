using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using AltLang.Domain.Grammar;
using Lang.Domain;
using Lang.GrammarTransform;

namespace Lang.Parser.LRAutomata;

public record Lr1<TPoint>(TPoint Core, ImmutableHashSet<Terminal> Terminals) : IPoint<Lr1<TPoint>>
    where TPoint : IPoint<TPoint>
{
    public virtual bool Equals(Lr1<TPoint>? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Core.Equals(other.Core) && Terminals.SetEquals(other.Terminals);
    }

    public override int GetHashCode() => Terminals.Aggregate(Core.GetHashCode(), HashCode.Combine);

    // public override string ToString() => $"{Core}; {string.Join(",", Terminals)}";
    public override string ToString() => $"({Core}); [{string.Join(",", Terminals)}]";
    public IRule Rule => Core.Rule;
    public int Pointer => Core.Pointer;
    public static Lr1<TPoint> FromRule(IRule rule) => new(TPoint.FromRule(rule), [Terminal.End]);

    public static bool TryCombine(Lr1<TPoint> a, Lr1<TPoint> b, [MaybeNullWhen(false)] out Lr1<TPoint> result)
    {
        result = default;
        if (!TPoint.TryCombine(a.Core, b.Core, out var coreResult)) return false;
        result = new Lr1<TPoint>(coreResult, a.Terminals.Union(b.Terminals));
        return true;
    }

    public IEnumerable<Token> GetLookaheadTokens() => Rule.Tokens[(Pointer + 1)..];
    public Lr1<TPoint> ShiftPoint() => this with {Core = Core.ShiftPoint()};

    public IEnumerable<Lr1<TPoint>> Closure(IGrammar grammar, FirstSet firstSet)
    {
        var lookahead = GetLookaheadTerminals(GetLookaheadTokens(), Terminals, firstSet);
        return Core.Closure(grammar, firstSet).Select(rule => new Lr1<TPoint>(rule, lookahead));
    }

    public string ToKey() => Core.ToKey();

    public Token GetCurrentToken() =>
        Pointer == Rule.Tokens.Count
            ? Terminal.End
            : Rule.Tokens[Pointer];

    private static ImmutableHashSet<Terminal> GetLookaheadTerminals(IEnumerable<Token> tokens,
        ImmutableHashSet<Terminal> terminals,
        FirstSet firstSet)
    {
        var result = firstSet.GetFirstSet(tokens);
        if (!result.Contains(Terminal.Lambda)) return result.ToImmutableHashSet();
        result.Remove(Terminal.Lambda);
        result.UnionWith(terminals);
        return result.ToImmutableHashSet();
    }
}