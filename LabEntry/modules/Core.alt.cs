using AltLang;
using LabEntry.domain;
using System;
using System.Collections.Generic;
using System.Linq;

public record CoreModule : ILangModule
{
    public string ModuleName => "Core";

    public SemanticObject? Reduce(SemanticObject node, Func<SemanticObject, SemanticObject> reduce)
    {
        if (node is not Structure s) return node;
        return s.Name switch
        {
            "Application" => ReduceApplication(s["function"], s["argument"], reduce),
            "Property" => ReducePropertyApplication(s["object"], s["property"]),
            "Match" => ReduceMatch(s["cases"], reduce(s["argument"]), reduce),
            "Object" or "Tuple" or "Template" or "And" or "Or" => s with
            {
                Children = s.Children.ToDictionary(p => p.Key, p => reduce(p.Value))
            },
            "Function" => node,
            _ => null
        };
    }

    private SemanticObject ReduceMatch(SemanticObject cases, SemanticObject arg,
        Func<SemanticObject, SemanticObject> reduce)
    {
        if (cases is not Structure s) throw new Exception();
        foreach (var (_, func) in s.Children.OrderBy(p => p.Key))
        {
            if (func is not Structure {Name: "Function"} f)
                throw new Exception();
            var vars = Match(reduce(f["template"]), arg);
            if (vars is null) continue;
            return reduce(ReplaceVars(f["body"], vars));
        }

        throw new Exception();
    }

    private SemanticObject ReducePropertyApplication(SemanticObject obj, SemanticObject prop)
    {
        if (prop is not Word {Name: { } word}) throw new Exception();
        if (obj is not Structure s) throw new Exception();
        if (!s.Children.TryGetValue(word, out var value)) throw new Exception();
        return value;
    }

    private SemanticObject? ReduceApplication(SemanticObject func, SemanticObject arg,
        Func<SemanticObject, SemanticObject> reduce)
    {
        if (func is not Structure {Name: "Function"} s)
        {
            var reduced = reduce(func);
            if (reduced is not Structure {Name: "Function"} s2)
                return null;
            s = s2;
        }

        var vars = Match(reduce(s["template"]), arg) ?? throw new Exception($"{s["template"]} <<- {arg}");
        return reduce(ReplaceVars(s["body"], vars));
    }

    private SemanticObject ReplaceVars(SemanticObject body, Dictionary<string, SemanticObject> vars)
    {
        if (body is Word {Name: { } name})
            return vars.GetValueOrDefault(name, body);

        if (body is not Structure structure) throw new Exception();

        return structure with
        {
            Children = structure.Children.ToDictionary(p => p.Key, p => ReplaceVars(p.Value, vars))
        };
    }

    private Dictionary<string, SemanticObject>? Match(SemanticObject template, SemanticObject arg)
    {
        Console.WriteLine($"Try to match template: {template} with {arg}");
        if (template is not Structure s) throw new Exception();
        switch (s.Name)
        {
            case "Template":
                return MatchTemplate(s, arg);
            case "And":
                var left = Match(s["left"], arg);
                var right = Match(s["right"], arg);
                if (right is null) return null;
                return left?.Concat(right).GroupBy(p => p.Key)
                    .ToDictionary(g => g.Key, g => g.Last().Value);
            case "Or":
                var left2 = Match(s["left"], arg);
                var right2 = Match(s["right"], arg);
                return left2 ?? right2;
            default:
                throw new Exception();
        }
    }

    private Dictionary<string, SemanticObject>? MatchTemplate(Structure template, SemanticObject arg)
    {
        if (template["pattern"] is Word w)
        {
            return new Dictionary<string, SemanticObject>()
            {
                [w.Name] = arg
            };
        }

        var pattern = (template["pattern"] as Structure)!;
        if (arg is not Structure a) throw new Exception();

        if (!string.IsNullOrEmpty(pattern.Name) && pattern.Name != a.Name)
            return null;
        var strict = pattern.Name is not "";
        var dictionary = new List<KeyValuePair<string, SemanticObject>>();
        foreach (var (key, value) in pattern.Children)
        {
            if (!a.Children.ContainsKey(key)) return null;
            var res = Match(value, a[key]);
            if (res == null) return null;
            dictionary.AddRange(res);
        }

        if (strict && a.Children.Keys.Except(pattern.Children.Keys).Any())
            return null;
        return dictionary.GroupBy(p => p.Key)
            .ToDictionary(g => g.Key, g => g.Last().Value);
    }
}