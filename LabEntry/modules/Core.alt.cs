using AltLang;
using LabEntry.domain;
using System;
using System.Collections.Generic;
using System.Linq;
using AltLang.Domain.Semantic;
using static AltLang.Domain.Semantic.Constructor;
using Function = LabEntry.domain.Function;
using Word = LabEntry.domain.Word;

public record CoreModule : ILangModule
{
    public string ModuleName => "Core";

    public void Register(ModuleContext context)
    {
        context.ModuleToReduce.Register(AsTemplate(Application(Function(Any, Any), Any)), this);
        context.ModuleToReduce.Register(AsTemplate(Application(Any, Any)), this);
        context.ModuleToReduce.Register(AsTemplate(Property(Any, Any)), this);
        context.ModuleToReduce.Register(AsTemplate(Constructor.Match(Any, Any)), this);
        context.ModuleToReduce.Register(AsTemplate(TemplateRec(Any)), this);

        context.ModuleToMatch.Register(AsTemplate(Template(Any)), this);
        context.ModuleToMatch.Register(AsTemplate(TemplateRec(Any)), this);
        context.ModuleToMatch.Register(AsTemplate(And(Any, Any)), this);
        context.ModuleToMatch.Register(AsTemplate(Or(Any, Any)), this);

        context.ModuleToSubstitute.Register(AsTemplate(Function(Any, Any)), this);
        context.ModuleToSubstitute.Register(AsTemplate(Property(Any, Any)), this);
    }

    #region Reduce

    public SemanticObject? Reduce(SemanticObject node, ModuleContext context)
    {
        Console.WriteLine($"[{ModuleName}][reduce] {Print(node)}");
        return node switch
        {
            Structure {Name: "Application"} s => ReduceApplication(s["function"], s["argument"], context),
            Structure {Name: "Property"} s => ReducePropertyApplication(s["object"], s["property"], context),
            Structure {Name: "Match"} s => ReduceMatch(s["cases"], context.Reduce(s["argument"]), context),
            Structure {Name: "TemplateRec"} s => ReduceTemplateRec(s["pattern"], context),
            _ => null
        };
    }

    private static Structure ReduceTemplateRec(SemanticObject obj, ModuleContext context) =>
        Template(obj switch
        {
            Structure s => ApplyToChildren(s, k => context.Reduce(TemplateRec(k))),
            Word w => w,
            _ => throw new NotImplementedException(),
        });

    private SemanticObject? ReduceApplication(SemanticObject function, SemanticObject argument, ModuleContext context)
    {
        if (function is not Structure {Name: "Function"} s)
        {
            var reducedFunction = context.Reduce(function);
            return context.Reduce(Application(reducedFunction, argument));
        }

        var reducedTemplate = context.Reduce(s["template"]);
        var vars = context.Match(reducedTemplate, argument) ?? throw new Exception($"{Print(s["template"])} <<- {Print(argument)}");
        return context.Reduce(context.Substitute(s["body"], vars));
    }

    private SemanticObject? ReducePropertyApplication(SemanticObject obj, SemanticObject property, ModuleContext context)
    {
        obj = context.Reduce(obj);
        if (property is not Word {Name: { } word}) return null;
        if (obj is not Structure s) return null;
        if (!s.Children.TryGetValue(word, out var value)) throw new Exception($"{Print(obj)} . {word}");
        return value;
    }

    private SemanticObject? ReduceMatch(SemanticObject cases, SemanticObject argument, ModuleContext context)
    {
        if (cases is not Structure s) throw new Exception();
        foreach (var (_, func) in s.Children.OrderBy(p => p.Key))
        {
            if (func is not Structure {Name: "Function"} f)
                throw new Exception();
            var vars = context.Match(context.Reduce(f["template"]), argument);
            if (vars is null) continue;
            return context.Reduce(context.Substitute(f["body"], vars));
        }

        return null;
    }

    #endregion

    #region Match

    public Dictionary<string, SemanticObject>? Match(SemanticObject template, SemanticObject obj, ModuleContext context)
    {
        Console.WriteLine($"[{ModuleName}][match] {Print(template)} <<= {Print(obj)}");
        return template switch
        {
            Structure {Name: "Template"} s => MatchTemplate(s, obj, context),
            Structure {Name: "RecTemplate"} s => MatchTemplate(context.Reduce(s) as Structure, obj, context),
            Structure {Name: "And"} s => (context.Match(s["left"], obj), context.Match(s["right"], obj)) switch
            {
                (null, _) => null,
                (_, null) => null,
                var (left, right) => left.Concat(right).GroupBy(p => p.Key)
                    .ToDictionary(g => g.Key, g => g.Last().Value)
            },
            Structure {Name: "Or"} s => context.Match(s["left"], obj) ?? context.Match(s["right"], obj),
            _ => null
        };
    }

    private Dictionary<string, SemanticObject>? MatchTemplate(Structure template, SemanticObject obj,
        ModuleContext context)
    {
        return template["pattern"] switch
        {
            Word {Name: { } name} => new Dictionary<string, SemanticObject> {[name] = obj},
            Structure s => obj switch
            {
                Structure o when string.IsNullOrEmpty(s.Name) || o.Name == s.Name => MatchStructure(s, o, context),
                _ => null
            }
        };

        Dictionary<string, SemanticObject>? MatchStructure(Structure pattern, Structure obj, ModuleContext context)
        {
            var strict = pattern.Name is not "";
            var dictionary = new List<KeyValuePair<string, SemanticObject>>();
            foreach (var (key, value) in pattern.Children)
            {
                if (!obj.Children.ContainsKey(key)) return null;
                var res = context.Match(value, obj[key]);
                if (res == null) return null;
                dictionary.AddRange(res);
            }

            if (strict && obj.Children.Keys.Except(pattern.Children.Keys).Any())
                return null;
            return dictionary.GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.Last().Value);
        }
    }

    #endregion

    #region Substitute

    private HashSet<string> GetEscapedVars(SemanticObject template) =>
        template switch
        {
            Structure {Name: "TemplateRec"} s => s["pattern"] switch
            {
                Word w => [w.Name],
                Structure ss => ApplyToChildren(ss, TemplateRec).Children.Values.SelectMany(GetEscapedVars).ToHashSet()
            },
            Structure {Name: "Template"} s => s["pattern"] switch
            {
                Word w => [w.Name],
                Structure {Children: { } c} => c.Values.SelectMany(GetEscapedVars).ToHashSet(),
                _ => []
            },
            Structure {Name: "Or" or "And", Children: { } c} =>
                c.Values.SelectMany(GetEscapedVars).ToHashSet(),
            _ => []
        };

    public SemanticObject? Substitute(SemanticObject obj, Dictionary<string, SemanticObject> vars,
        ModuleContext context)
    {
        Console.WriteLine($"[{ModuleName}][substitute] {Print(obj)} <<- {Print(new Structure("", vars))}");
        return obj switch
        {
            Structure {Name: "Function"} s => SubstituteFunction(s, vars, context),
            Structure {Name: "Property"} s => Property(context.Substitute(s["object"], vars), s["property"]),
            _ => null
        };

        SemanticObject? SubstituteFunction(Structure function, Dictionary<string, SemanticObject> vars,
            ModuleContext context)
        {
            var escaped = GetEscapedVars(function["template"]);
            var allowedVars = vars.Where(v => !escaped.Contains(v.Key)).ToDictionary();
            return ApplyToChildren(function, c => context.Substitute(c, allowedVars));
        }
    }

    #endregion
}