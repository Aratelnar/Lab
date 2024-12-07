namespace Lang.Domain;

public record Terminal(string Token, TerminalType Type) : Token
{

    public override string ToString() => $"{Type}:{Token}";
    public static Terminal Keyword(string token) => new (token, TerminalType.Keyword);
    public static Terminal Word(string token) => new (token, TerminalType.Word);
    public static Terminal End = new("-|", TerminalType.Internal);
    public static Terminal Lambda = new("\\", TerminalType.Internal);
};

public enum TerminalType
{
    Internal,
    Keyword,
    Word
}