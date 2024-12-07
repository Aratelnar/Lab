﻿using Lang.Domain;

namespace Lang.GrammarTransform;

public class FollowSet 
{
    private FirstSet FirstSet;
    private Dictionary<NonTerminal, HashSet<Terminal>> Follow;

    public FollowSet(Grammar grammar)
    {
        FirstSet = new FirstSet(grammar);
        Follow = new Dictionary<NonTerminal, HashSet<Terminal>>();
        foreach (var nonTerminal in grammar.GetAllSources())
        {
            Follow[nonTerminal] = new HashSet<Terminal>();
        }

        ConstructFollow(grammar);
    }

    private void ConstructFollow(Grammar grammar)
    {
        var changed = true;
        Follow[grammar.Axiom].Add(Terminal.End);
        while (changed)
        {
            changed = false;

            foreach (var rule in grammar.Rules)
            {
                var tokens = rule.Tokens.ToArray();
                for (var index = rule.Tokens.Count - 1; index >= 0; index--)
                {
                    var token = tokens[index];
                    if (token is not NonTerminal nonTerminal) continue;
                    var count = Follow[nonTerminal].Count;
                    var forFirst = tokens[(index + 1)..].ToList();
                    var next = forFirst.Count > 0
                        ? FirstSet.GetFirstSet(forFirst)
                        : new HashSet<Terminal> {Terminal.Lambda};
                    Follow[nonTerminal].UnionWith(next.Except(new[] {Terminal.Lambda}));
                    if (next.Contains(Terminal.Lambda))
                    {
                        Follow[nonTerminal].UnionWith(Follow[rule.Source]);
                    }

                    changed |= Follow[nonTerminal].Count > count;
                }
            }
        }
    }

    public HashSet<Terminal> GetFollowSet(NonTerminal token)
    {
        return Follow[token];
    }
}