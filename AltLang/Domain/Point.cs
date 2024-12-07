namespace Lang.Domain;

public record Point(Rule Rule, int Pointer)
{
    public static Point FromRule(Rule rule) => new(rule, 0);

    public Token GetCurrentToken() =>
        Pointer == Rule.Tokens.Count 
            ? Terminal.End 
            : Rule.Tokens[Pointer];

    public bool ShouldMakeClosure() => GetCurrentToken() is NonTerminal;
    public Point ShiftPoint() => this with {Pointer = Pointer == Rule.Tokens.Count ? Pointer : Pointer + 1};
    public override string ToString()
    {
        var tokens = Rule.Tokens.Select(t=>t.ToString()).ToList();
        tokens.Insert(Pointer, "•");
        return $"{Rule.Source} -> {string.Join(" ", tokens)}";
    }

    public IEnumerable<Token> GetLookaheadTokens() => Rule.Tokens[(Pointer + 1)..];
};