using System.Text.RegularExpressions;
using Lang.Domain;

namespace Lang.Lexer;

public class Lexer
{
    public Lexer() => States.Add(new State());
    private List<State> States { get; init; } = new();

    private record State
    {
        public Dictionary<char, int> Transitions { get; init; } = new();
        public Action? Action { get; set; }
    }

    public record Action(bool Final, Func<string, Terminal> GetToken);

    public static Lexer FromGrammar(Grammar grammar)
    {
        var lexer = new Lexer();
        var keywords = grammar.Rules.SelectMany(r => r.Tokens)
            .Where(t => t is Terminal {Type: TerminalType.Keyword}).Cast<Terminal>();
        foreach (var terminal in keywords) lexer.AddWord(terminal.Token);
        return lexer;
    }

    public static Lexer FromKeywords(IEnumerable<string> keywords)
    {
        var lexer = new Lexer();
        foreach (var keyword in keywords) lexer.AddWord(keyword);
        return lexer;
    }

    private static Predicate<char> IsWord = c =>
        c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_';

    public IEnumerable<Terminal> Read(string text)
    {
        var q = 0;
        var wordStart = 0;
        for (var index = 0; index < text.Length + 1;)
        {
            var c = index < text.Length ? text[index] : ' ';
            if (char.IsWhiteSpace(c) && q == 0)
            {
                index++;
                wordStart = index;
                continue;
            }

            var state = States[q];
            if (state.Transitions.TryGetValue(c, out var next))
            {
                q = next;
                index++;
                continue;
            }

            if (state.Action is { } action && (action.Final || !char.IsLetterOrDigit(c)))
            {
                yield return action.GetToken(text[wordStart..index]);
                wordStart = index;
                q = 0;
                continue;
            }

            var word = ReadWord(text, ref wordStart);
            index = wordStart;
            yield return Terminal.Word(word);
            q = 0;
        }
    }

    private string ReadWord(string text, ref int pos)
    {
        var curr = pos;
        for (; curr < text.Length && IsWord(text[curr]); curr++) ;
        var readWord = text[pos..curr];
        pos = curr;
        return readWord;
    }

    private void AddWord(string terminalToken)
    {
        var stateInd = 0;
        foreach (var c in terminalToken)
        {
            var state = States[stateInd];
            if (!state.Transitions.TryGetValue(c, out var next))
            {
                States.Add(new State());
                next = state.Transitions[c] = States.Count - 1;
            }

            stateInd = next;
        }

        States[stateInd].Action = new Action(!terminalToken.All(char.IsLetter), Terminal.Keyword);
    }
}