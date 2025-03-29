using AltLang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AltLang.Domain;
using AltLang.Domain.Semantic;
using AltLang.Domain.Semantic.Explicit;
using Lang.Domain.TypeSystem;
using Lang.Util;
using static AltLang.Domain.Constructor;
using Assump = System.Collections.Generic.Dictionary<string, AltLang.Domain.Semantic.Explicit.Term>;
using Structure = AltLang.Domain.Semantic.Structure;
using Word = AltLang.Domain.Semantic.Word;

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

        context.ModuleToInfer.Register(AsTemplate(Template(Any)), this);
        context.ModuleToInfer.Register(AsTemplate(TemplateRec(Any)), this);
        context.ModuleToInfer.Register(AsTemplate(Function(Any, Any)), this);
        context.ModuleToInfer.Register(AsTemplate(Application(Any, Any)), this);
        context.ModuleToInfer.Register(AsTemplate(Property(Any, Any)), this);
        context.ModuleToInfer.Register(AsTemplate(Constructor.Match(Any, Any)), this);

        context.ModuleToSubstitute.Register(AsTemplate(Function(Any, Any)), this);
        context.ModuleToSubstitute.Register(AsTemplate(Property(Any, Any)), this);
    }

    #region Reduce

    public SemanticObject? Reduce(SemanticObject node, ModuleContext context)
    {
        context.Log($"[{ModuleName}][reduce] {node.ToTerm().Print()}");
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
            function = reducedFunction;
            if (function is not Structure {Name: "Function"} ss)
            {
                reducedFunction = context.Reduce(function);
                function = reducedFunction;
                if (function is not Structure {Name: "Function"} sss)
                {
                    return null;
                }

                ss = sss;
            }

            s = ss;
        }

        var reducedTemplate = context.Reduce(s["template"]);
        var vars = context.Match(reducedTemplate, argument) ??
                   throw new Exception($"{s["template"].ToTerm().Print()} <<- {argument.ToTerm().Print()}");
        return context.Reduce(context.Substitute(s["body"], vars));
    }

    private SemanticObject? ReducePropertyApplication(SemanticObject obj, SemanticObject property,
        ModuleContext context)
    {
        obj = context.Reduce(obj);
        if (property is not Word {Name: { } word}) return null;
        if (obj is not Structure s) return null;
        if (!s.Children.ToDictionary().TryGetValue(word, out var value))
            throw new Exception($"{obj.ToTerm().Print()} . {word}");
        return value;
    }

    private SemanticObject? ReduceMatch(SemanticObject cases, SemanticObject argument, ModuleContext context)
    {
        if (cases is not Structure s) throw new Exception();
        foreach (var (_, func) in s.Children.OrderBy(p => p.Key))
        {
            if (func is not Structure {Name: "Function"} f)
                throw new Exception();
            var vars = context.Match(context.Reduce(f["template"]), context.Reduce(argument));
            if (vars is null) continue;
            return context.Reduce(context.Substitute(f["body"], vars));
        }

        return null;
    }

    #endregion

    #region Match

    public Dictionary<string, SemanticObject>? Match(SemanticObject template, SemanticObject obj, ModuleContext context)
    {
        context.Log($"[{ModuleName}][match] {template.ToTerm().Print()} <<= {obj.ToTerm().Print()}");
        var match = template switch
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
        if (match is null)
        {
            obj = context.Reduce(obj);
            match = template switch
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

        return match;
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
    }

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

    #endregion

    #region Infer

    public (Term, Assump)? Infer(SemanticObject obj, Assump assump, ModuleContext context)
    {
        return obj switch
        {
            Structure {Name: "Function"} s => InferFunction(s, assump, context),
            Structure {Name: "Application"} s => InferApplication(s, assump, context),
            Structure {Name: "Template"} s => InferTemplate(s, assump, context),
            Structure {Name: "TemplateRec"} s => InferTemplate(context.Reduce(s), assump, context),
            Structure {Name: "Match"} s => InferMatch(s, assump, context),
        };
    }

    private (Term, Assump)? InferMatch(Structure match, Assump assump, ModuleContext context)
    {
        var (argType, _) = context.Infer(match["argument"], assump) ?? throw new TypeException();
        Term? matchInputType = null;
        Term? matchResultType = null;
        var cases = (match["cases"] as Structure)!.Children.Values;
        foreach (var c in cases)
        {
            var (funcType, _) = context.Infer(c, assump) ?? throw new TypeException();
            var funcTypeAbs = (funcType as Function)!;
            matchInputType = matchInputType is null
                ? funcTypeAbs.Type
                : new Or(matchInputType, funcTypeAbs.Type);
            matchResultType = matchResultType is null
                ? funcTypeAbs.Result
                : new Or(matchResultType, funcTypeAbs.Result);
        }

        context.TypeResolver.Unify(matchInputType!, argType);
        return (matchResultType!, []);
    }

    private (Term, Assump) InferFunction(Structure structure, Assump assumptions, ModuleContext context)
    {
        var (inputType, localAssumptions) =
            context.Infer(structure["template"], assumptions) ?? throw new TypeException();
        var combinedAssumptions = LinqExtensions.Combine(localAssumptions, assumptions);
        var (resultType, ass) = context.Infer(structure["body"], combinedAssumptions) ?? throw new TypeException();
        return (new Function(inputType, resultType), ass);
    }

    private (Term, Assump) InferApplication(Structure structure, Assump assumptions, ModuleContext context)
    {
        var (funcType, _) = context.Infer(structure["function"], assumptions) ?? throw new TypeException();
        var (argType, _) = context.Infer(structure["argument"], assumptions) ?? throw new TypeException();
        var inType = context.TypeResolver.NewVar();
        var resType = context.TypeResolver.NewVar();
        context.TypeResolver.Unify(new Function(inType, resType), funcType);
        context.TypeResolver.Unify(inType, argType);
        return (resType, []);
    }

    private (Term inputType, Assump localAssumptions)? InferTemplate(SemanticObject template, Assump assumptions,
        ModuleContext context)
    {
        if (template is not Structure {Name: "Template"} s) throw new TypeException();
        switch (s["pattern"])
        {
            case Word w:
                var v = context.TypeResolver.NewVar();
                var a = new Assump {[w.Name] = v};
                return (v, a);
            case Structure s1:
                Assump a2 = [];
                var c3 = new Dictionary<string, Term>();
                foreach (var (key, val) in s1.Children)
                {
                    var (t1, a3) = context.Infer(val, assumptions) ?? throw new TypeException();
                    a2 = LinqExtensions.Combine(a3, a2);
                    c3.Add(key, t1);
                }

                var t = new StructureTemplate(s1.Name, c3);

                return (t, a2);
            default:
                return null;
        }
    }

    #endregion

    #region Substitute

    public SemanticObject? Substitute(SemanticObject obj, Dictionary<string, SemanticObject> vars,
        ModuleContext context)
    {
        context.Log(
            $"[{ModuleName}][substitute] {obj.ToTerm().Print()} <<- {new Unknown("", vars.Map(p => p.ToTerm())).Print()}");
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

    #endregion
}