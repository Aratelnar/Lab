namespace Lang.Domain;

public record NonTerminal(string Name) : Token
{
    public override string ToString() => Name;
}