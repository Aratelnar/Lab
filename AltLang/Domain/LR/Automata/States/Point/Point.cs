using AltLang.Domain.Grammar;
using Lang.GrammarTransform;
using Lang.Parser.LRAutomata;

namespace Lang.Domain;

public record Point(IRule Rule, int Pointer) : IPoint<Point>
{
    public static Point FromRule(IRule rule) => new(rule, 0);
    public static bool TryCombine(Point a, Point b, out Point result)
    {
        result = a;
        return a == b;
    }


    public Point ShiftPoint() => this with {Pointer = Pointer == Rule.Tokens.Count ? Pointer : Pointer + 1};
    public IEnumerable<Point> Closure(IGrammar grammar, FirstSet firstSet)
    {
        var token = GetCurrentToken() as NonTerminal;
        return grammar.GetRulesBySource(token!).Select(FromRule);
    }

    public string ToKey() => ToString();

    public Token GetCurrentToken() =>
        Pointer == Rule.Tokens.Count
            ? Terminal.End
            : Rule.Tokens[Pointer];

    public override string ToString()
    {
        var tokens = Rule.Tokens.Select(t=>t.ToString()).ToList();
        tokens.Insert(Pointer, "•");
        return $"{Rule.Source} -> {string.Join(" ", tokens)}";
    }

};