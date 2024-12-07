using LabEntry.domain;

namespace AltLang;

public record AltModule(string ModuleName, SemanticObject Reducers) : ILangModule
{
    public SemanticObject? Reduce(SemanticObject obj, Func<SemanticObject, SemanticObject> reduce)
    {
        if (Reducers is not Structure {Children: { } c}) return null;
        foreach (var reducer in c.Values)
        {
            if(reducer is not Structure {Name:"Reduce", Children: {} cc}) continue;
            if(!cc.TryGetValue("rule", out var rule)) continue;
            var reduced = TryReduceApplication(rule, obj, reduce);
            if(reduced is null) continue;
            return reduce(reduced);
        }

        return null;
    }

    private SemanticObject? TryReduceApplication(SemanticObject func, SemanticObject arg,
        Func<SemanticObject, SemanticObject> reduce)
    {
        if (func is not Structure {Name: "Function"} s)
        {
            var reduced = reduce(func);
            if (reduced is not Structure {Name: "Function"} s2)
                throw new Exception();
            s = s2;
        }

        var vars = Match(s["template"], arg);
        return vars != null ? reduce(ReplaceVars(s["body"], vars)) : null;
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