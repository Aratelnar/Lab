using AltLang.Domain.Semantic;
using Lang.Domain;
using Lang.Domain.Semantic;
using Lang.Parser;
using Lang.RuleReader.Semantic;

namespace Lang.RuleReader;

public static class ModuleReader
{
    public static Module ReadModule(string moduleName, string module)
    {
        var lines = module.Split('\n').Select(l => l.Trim());
        var imports = new List<string>();
        var rules = new HashSet<SemanticRule>();
        var priority = 0;
        foreach (var line in lines)
        {
            if (line.StartsWith("import", StringComparison.OrdinalIgnoreCase))
                imports.Add(line.Split(" ", StringSplitOptions.RemoveEmptyEntries).Last());
            else if (line.StartsWith("rule", StringComparison.OrdinalIgnoreCase))
                rules.Add(ReadRule(line[4..], priority++));
        }

        var automata = AutomataBuilder.FromSemanticGrammar(new SemanticGrammar(new NonTerminal("Expr"), rules));
        return new Module(moduleName, imports, automata);
    }

    public static SemanticRule ReadRule(string ruleDefinition, int priority)
    {
        var items = ruleDefinition.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var state = "source";
        NonTerminal source = null!;
        var tokens = new List<Token>();
        SemanticObjectDefinition definition = null!;
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

        return new SemanticRule(new Rule(source, tokens, priority), definition);
    }

    private static Token GetToken(string item)
    {
        if (item == "word") return Terminal.Word("");
        if (item.StartsWith('\'')) return Terminal.Keyword(item[1..^1]);
        return new NonTerminal(item);
    }
}