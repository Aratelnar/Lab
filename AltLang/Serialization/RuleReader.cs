using System.Text.RegularExpressions;
using AltLang.Domain.Grammar;
using AltLang.Domain.Grammar.Rules;
using AltLang.Domain.Semantic;
using AltLang.Serialization.Semantic;
using Lang.Domain;

namespace AltLang.Serialization;

public class RuleReader
{
    private static Regex Whitespace = new(@"\s", RegexOptions.Compiled);

    public static Prioritized<Rule> Read(string line)
    {
        var split = Whitespace.Split(line);
        return CreateRule(split);
    }

    private static Prioritized<Rule> CreateRule(string[] tokens)
    {
        var priority = short.Parse(tokens[0]);
        tokens = tokens[1..];
        if (tokens[1] != ":") throw new ArgumentException();
        var source = new NonTerminal(tokens[0]);
        var t = tokens[2..].Select(s => s[0] == '\''
                ? (Token) Terminal.Keyword(s[1..^1])
                : s switch
                {
                    "word" => Terminal.Word(""),
                    "number" => Terminal.Number(""),
                    _ => new NonTerminal(s),
                })
            .ToList();
        return new Prioritized<Rule>(new Rule(source, t), new Priority(priority));
    }

    public static Semantic<Prioritized<Rule>> ReadSemantic(string line)
    {
        var split = line.Split(" | ", 2, StringSplitOptions.TrimEntries);
        var core = CreateRule(Whitespace.Split(split[0]));
        var def = ObjectDefinitionReader.Read(split[1]);
        return new Semantic<Prioritized<Rule>>(core, def);
    }

    private static SemanticObject BuildObject(ObjectDefinition definition, IList<SemanticObject> nodes) =>
        definition switch
        {
            WordDefinition wordDefinition => BuildWord(wordDefinition, nodes),
            StructureDefinition structureDefinition => BuildStructure(structureDefinition, nodes),
            _ => throw new ArgumentOutOfRangeException(nameof(definition), definition, null)
        };

    private static SemanticObject BuildStructure(StructureDefinition definition, IList<SemanticObject> nodes) =>
        definition switch
        {
            ExplicitStructureDefinition expl => new Structure(BuildWord(expl.Name, nodes).Name,
                BuildProperties(expl.Properties, nodes)),
            StructureNumberDefinition number => nodes[number.Number],
            _ => throw new ArgumentOutOfRangeException(nameof(definition), definition, null)
        };

    private static Dictionary<string, SemanticObject> BuildProperties(PropertyDefinition[] properties,
        IList<SemanticObject> nodes)
    {
        var result = new Dictionary<string, SemanticObject>();
        foreach (var propertyDefinition in properties)
        {
            string? key;
            SemanticObject? obj;
            switch (propertyDefinition)
            {
                case ExplicitPropertyDefinition expl:
                    key = BuildWord(expl.Key, nodes).Name;
                    obj = BuildObject(expl.Object, nodes);
                    result[key] = obj;
                    break;
                case NamelessPropertyDefinition nameless:
                    key = $"_{result.Count}";
                    obj = BuildObject(nameless.Object, nodes);
                    result[key] = obj;
                    break;
                case SpreadPropertyDefinition spread:
                    obj = nodes[spread.Number];
                    foreach (var (k, v) in (obj as Structure)!.Children)
                        result[k] = v;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(propertyDefinition));
            }
        }

        return result;
    }

    private static Word BuildWord(WordDefinition definition, IList<SemanticObject> nodes) =>
        definition switch
        {
            ExplicitWordDefinition expl => new Word(expl.Word),
            StructureNameDefinition name => nodes[name.Number] switch
            {
                Word w => w,
                Structure s => new Word(s.Name),
                _ => throw new ArgumentOutOfRangeException()
            },
            _ => throw new ArgumentOutOfRangeException(nameof(definition), definition, null)
        };
}