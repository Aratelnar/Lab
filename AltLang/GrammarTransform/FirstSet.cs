using System.Text;
using Lang.Domain;

namespace Lang.GrammarTransform;

public class FirstSet
{
    private readonly Grammar _grammar;
    private Dictionary<NonTerminal, HashSet<Terminal>> First;

    public FirstSet(Grammar grammar)
    {
        _grammar = grammar;
        First = new Dictionary<NonTerminal, HashSet<Terminal>>();
        foreach (var nonTerminal in grammar.GetAllSources())
        {
            First[nonTerminal] = new HashSet<Terminal>();
        }

        ConstructFirst(grammar);
    }

    private void ConstructFirst(Grammar grammar)
    {
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var rule in grammar.Rules)
            {
                var first = GetFirstSet(rule.Tokens);
                if(first.IsSubsetOf(First[rule.Source])) continue;
                First[rule.Source].UnionWith(first);
                changed = true;
            }
        }
    }

    public HashSet<Terminal> GetFirstSet(IEnumerable<Token> tokens)
    {
        var result = new HashSet<Terminal>();
        foreach (var token in tokens)
        {
            if (token is Terminal t)
            {
                result.Add(t);
                return result;
            }
            var first = First[token as NonTerminal];
            result.UnionWith(first);
            if (!first.Contains(Terminal.Lambda)) return result;
            result.Remove(Terminal.Lambda);
        }

        result.Add(Terminal.Lambda);
        return result;
    }
}