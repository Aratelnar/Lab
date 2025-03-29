using System.Diagnostics;
using System.Xml.Linq;
using AltLang.Domain.Grammar;
using AltLang.Domain.Grammar.Rules;
using AltLang.Domain.Semantic;
using AltLang.Serialization.Semantic;
using Lang.Domain;
using Lang.Domain.Semantic;
using Lang.Parser;

namespace Lang.RuleReader;

public static class ModuleReader
{
    public static Module ReadModule(string moduleName, string module)
    {
        var lines = module.Split('\n').Select(l => l.Trim()).ToArray();
        var imports = new List<string>(lines.Length);
        var rules = new HashSet<Semantic<Prioritized<Rule>>>();
        short priority = 0;

        foreach (var line in lines)
        {
            if (line.StartsWith("import", StringComparison.OrdinalIgnoreCase))
                imports.Add(line.Split(" ", StringSplitOptions.RemoveEmptyEntries).Last());
            else
            {
                if (line.StartsWith("rule", StringComparison.OrdinalIgnoreCase))
                {
                    var (prior, rule) = ReadOptions(line);

                    var semRule = ReadRule(rule);
                    rules.Add(new Semantic<Prioritized<Rule>>(
                        new Prioritized<Rule>(semRule.Core, prior ?? new Priority(priority++)), semRule.Reduce));
                }
            }
        }

        var automata =
            AutomataBuilder.FromSemanticGrammar(
                new Grammar<Semantic<Prioritized<Rule>>>(new NonTerminal("Expr"), rules));
        return new Module(moduleName, imports, automata);

        (Priority?, string) ReadOptions(string s)
        {
            s = s[4..];
            if (s[0] != '(') return (null, s);
            var i = s.IndexOf(')');
            var p = s.Substring(1, i - 1);
            return (new Priority(p.Split('.').Select(ParsePriorSeg).ToArray()), s[(i + 1)..]);
        }

        short ParsePriorSeg(string s)
        {
            if (s[^1] == 'r') return (short) -short.Parse(s[..^1]);
            return short.Parse(s);
        }
    }

    public static Semantic<Rule> ReadRule(string ruleDefinition)
    {
        var items = ruleDefinition.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var state = "source";
        NonTerminal source = null!;
        var tokens = new List<Token>();
        ObjectDefinition definition = null!;
        foreach (var item in items)
        {
            switch (state)
            {
                case "source":
                    source = new NonTerminal(item);
                    state = "colon";
                    break;
                case "colon":
                    if (item != ":") throw new Exception();
                    state = "token";
                    break;
                case "token":
                    if (item != "|") tokens.Add(GetToken(item));
                    else state = "semantic";
                    break;
                case "semantic":
                    state = "done";
                    definition = ObjectDefinitionReader.Read(item);
                    break;
                case "done":
                    continue;
            }
        }

        return new Semantic<Rule>(new Rule(source, tokens), definition);
    }

    private static Token GetToken(string item)
    {
        if (item == "word") return Terminal.Word("");
        if (item == "number") return Terminal.Number("");
        if (item.StartsWith('\'')) return Terminal.Keyword(item[1..^1]);
        return new NonTerminal(item);
    }
}