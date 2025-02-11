using Lang.Util.RecordCollections;

namespace AltLang.Domain.Grammar;

public interface IRule
{
    NonTerminal Source { get; }
    RecordList<Token> Tokens { get; }
    T Lift<T>() where T : IRule;

    string ToKey();
}