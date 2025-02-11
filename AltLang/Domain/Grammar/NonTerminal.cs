namespace AltLang.Domain.Grammar;

public record NonTerminal(string Name) : Token
{
    public override string ToString() => Name;
}