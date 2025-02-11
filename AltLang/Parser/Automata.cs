using AltLang.Domain.Grammar;
using AltLang.Domain.Grammar.Rules;
using Lang.Domain;

namespace Lang.Parser;

public record Automata
{
    public Dictionary<string, Rule> Rules { get; set; } = new();
    public readonly Dictionary<(int, Token), Action> Actions = new();

    public IEnumerable<string> Read(IList<Terminal> text)
    {
        var stack = new Stack<StackItem>();
        stack.Push(new StackItem(0, Terminal.Lambda));
        var input = new Stack<Token>(text.Reverse());
        while (true)
        {
            var token = input.Count > 0 ? input.Pop() : Terminal.End;
            var state = stack.Peek().State;
            if (!Actions.TryGetValue((state, token), out var action))
                throw new Exception();
            switch (action)
            {
                case Reduce reduce:
                    input.Push(token);
                    yield return TryReduce(reduce, stack, input);
                    break;
                case Shift shift:
                    stack.Push(new StackItem(shift.NextState, token));
                    break;
                case Accept:
                    yield break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }
    }

    private string TryReduce(Reduce reduce, Stack<StackItem> stack, Stack<Token> input)
    {
        var rule = Rules[reduce.Rule];
        for (var i = rule.Tokens.Count - 1; i >= 0; i--)
        {
            var item = stack.Pop();
            if (!rule.Tokens[i].Equals(item.Token)) throw new Exception();
        }

        input.Push(rule.Source);
        return reduce.Rule;
    }

    private record StackItem(int State, Token Token);

    public void PrettyPrint(FileStream file)
    {
        var writer = new StreamWriter(file);
        var tokens = Actions.Keys.Select(p => p.Item2).Distinct().ToList();
        var rules = Rules.Keys.ToList();
        writer.Write("\t | ");
        foreach (var token in tokens)
        {
            var name = token switch
            {
                Terminal t => $"'{t.Token}'",
                NonTerminal a => a.Name
            };
            writer.Write($"{name}\t | ");
        }
        writer.WriteLine();
        var q = Actions.Keys.Select(p => p.Item1).Max();
        for (var i = 0; i < q + 1; i++)
        {
            writer.Write($"{i}\t | ");
            foreach (var token in tokens)
            {
                if (!Actions.TryGetValue((i, token), out var action))
                {
                    writer.Write("  \t | ");
                    continue;
                }

                var text = action switch
                {
                    Shift s => $"🡐{s.NextState}",
                    Reduce r => $"o{rules.IndexOf(r.Rule)}",
                    Accept => "✓"
                };
                
                writer.Write($"{text}\t | ");
            }
            writer.WriteLine();
        }
        writer.Close();
    }
    
    public abstract record Action;

    public record Accept : Action;

    public record Shift(int NextState) : Action;

    public record Reduce(string Rule) : Action;
};

