using System.Diagnostics.CodeAnalysis;
using AltLang.Domain.Grammar;
using AltLang.Domain.Grammar.Rules;
using Lang.Domain;
using Lang.GrammarTransform;

namespace Lang.Parser.LRAutomata;

public record Prioritized<TPoint>(TPoint Core, Priority Priority) : IPoint<Prioritized<TPoint>>
    where TPoint : IPoint<TPoint>
{
    public virtual bool Equals(Prioritized<TPoint>? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Core.Equals(other.Core) && Priority.Equals(other.Priority);
    }

    public override int GetHashCode() =>
        HashCode.Combine(Core.GetHashCode(), Priority.GetHashCode());

    public override string ToString() => $"({Core}); {Priority}";
    public IRule Rule => Core.Rule;
    public int Pointer => Core.Pointer;
    public static Prioritized<TPoint> FromRule(IRule rule)
    {
        var rulePriority = rule.Lift<IRulePrioritized>().Priority;
        return new Prioritized<TPoint>(TPoint.FromRule(rule), rulePriority);
    }

    public static bool TryCombine(Prioritized<TPoint> a, Prioritized<TPoint> b,
        [MaybeNullWhen(false)] out Prioritized<TPoint> result)
    {
        result = default;
        if (!TPoint.TryCombine(a.Core, b.Core, out var coreResult)) return false;
        result = new Prioritized<TPoint>(coreResult, Priority.Min(a.Priority, b.Priority));
        return true;
    }

    public Prioritized<TPoint> ShiftPoint() => this with {Core = Core.ShiftPoint()};

    public IEnumerable<Prioritized<TPoint>> Closure(IGrammar grammar, FirstSet firstSet)
    {
        return Core.Closure(grammar, firstSet).Select(r => new Prioritized<TPoint>(r, GetPriority(r)));

        Priority GetPriority(TPoint point) => point.Rule.Lift<IRulePrioritized>().Priority;
    }

    public string ToKey() => Core.ToKey();

    public Token GetCurrentToken() =>
        Pointer == Rule.Tokens.Count
            ? Terminal.End
            : Rule.Tokens[Pointer];
}