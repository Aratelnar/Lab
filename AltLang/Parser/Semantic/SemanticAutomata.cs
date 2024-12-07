using LabEntry.domain;
using Lang.Domain;
using Lang.Domain.Semantic;
using Lang.RuleReader.Semantic;

namespace Lang.Parser.Semantic;

public record SemanticAutomata
{
    public List<SemanticRuleShort> Rules { get; set; } = new();
    public readonly Dictionary<(int, Token), Action> Actions = new();

    public NonTerminal Axiom { get; init; }
    public HashSet<Token> KnownTokens { get; set; } = new();

    public SemanticObject? Read(IList<Terminal> text)
    {
        var stack = new Stack<State>();
        stack.Push(new State(0, Terminal.Lambda, null));
        for (var pos = 0; pos <= text.Count;)
        {
            var token = pos == text.Count ? Terminal.End : text[pos];
            var state = stack.Peek().Position;
            if (!Actions.TryGetValue((state, token), out var action) &&
                !Actions.TryGetValue((state, Terminal.Word("")), out action))
                throw new Exception();
            switch (action)
            {
                case Reduce:
                    TryReduce(stack, token);
                    break;
                case Shift:
                    TryShift(stack, token);
                    pos++;
                    break;
                case Accept:
                    goto End;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        End:
        var resultState = stack.Pop();
        return resultState.Data;
    }

    private void TryReduce(Stack<State> stack, Token token)
    {
        var reduce = GetAction(stack, token) as Reduce;
        var rule = Rules[reduce!.Rule];
        var data = new List<SemanticObject>();
        for (var i = rule.Count - 1; i >= 0; i--)
        {
            var item = stack.Pop();
            data.Add(item.Data ?? new Word((item.Token as Terminal)!.Token));
        }

        TryShift(stack, rule.Source, BuildObject(rule.Reduce, data));
    }

    private void TryShift(Stack<State> stack, Token token, SemanticObject? data = null)
    {
        var shift = GetAction(stack, token) as Shift;
        stack.Push(new State(shift!.NextState, token, data));
    }

    private Action GetAction(Stack<State> stack, Token token)
    {
        var state = stack.Peek();
        return Actions.TryGetValue((state.Position, token), out var action) ||
               Actions.TryGetValue((state.Position, Terminal.Word("")), out action)
            ? action
            : throw new Exception();
    }

    private record State(int Position, Token Token, SemanticObject? Data);

    public bool ShouldClosure(int state) => Actions.Keys.Where(k => k.Item1 == state).Any(k => k.Item2 == Axiom);

    public abstract record Action(int Priority);

    public record Accept() : Action(0);

    public record Shift(int NextState, int Priority) : Action(Priority);

    public record Reduce(int Rule, int Priority) : Action(Priority);

    private static SemanticObject BuildObject(SemanticObjectDefinition definition, IList<SemanticObject> nodes) =>
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
};

public record SemanticRuleShort(NonTerminal Source, byte Count, SemanticObjectDefinition Reduce);