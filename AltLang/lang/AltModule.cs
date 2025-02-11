using AltLang.Domain.Semantic;
using LabEntry.domain;

namespace AltLang;

public record AltModule : ILangModule
{
    public AltModule(string ModuleName, SemanticObject Reducers)
    {
        this.ModuleName = ModuleName;
        this.Reducers = (Reducers as Structure)?.Children.Values.ToList() ?? [];
        return;
    }

    #region Reduce

    public void Register(ModuleContext context)
    {
        foreach (var reducer in Reducers)
        {
            switch (reducer)
            {
                case Structure {Name: "Reduce", Children: { } cc}:
                    if (!cc.TryGetValue("rule", out var rule)) continue;
                    if (rule is not Structure {Name: "Function"} s) continue;
                    context.ModuleToReduce.Register(TypeObject.FromSemanticObject(s["template"]), this);
                    break;
                case Structure {Name: "Define", Children: { } cc2}:
                    if (!cc2.TryGetValue("name", out var name) || name is not Word w) continue;
                    context.ModuleToReduce.RegisterWord(w, this);
                    break;
            }
        }
    }

    public SemanticObject? Reduce(SemanticObject obj, ModuleContext context)
    {
        Console.WriteLine($"[{ModuleName}][reduce] {Constructor.Print(obj)}");
        foreach (var reducer in Reducers)
        {
            switch (reducer)
            {
                case Structure {Name: "Reduce", Children: { } cc}:
                    if (!cc.TryGetValue("rule", out var rule)) continue;
                    var result = TryApplyApplication(rule, obj, context);
                    if (result is null) continue;
                    return context.Reduce(result);
                case Structure {Name: "Define", Children: { } cc2}:
                    if (!cc2.TryGetValue("name", out var name) || name is not Word w) continue;
                    if (!cc2.TryGetValue("value", out var val)) continue;
                    if (obj is Word w2 && w2.Name == w.Name) return val;
                    break;
            }
        }

        return null;
    }

    private SemanticObject? TryApplyApplication(SemanticObject function, SemanticObject arg, ModuleContext context)
    {
        if (function is not Structure {Name: "Function"} s)
        {
            var reducedFunction = context.Reduce(function);
            if (reducedFunction is not Structure {Name: "Function"} s2)
                return null;
            s = s2;
        }

        var vars = context.Match(s["template"], arg);
        return vars != null ? context.Substitute(s["body"], vars) : null;
    }

    #endregion

    #region Match

    public Dictionary<string, SemanticObject>? Match(SemanticObject template, SemanticObject obj, ModuleContext context)
    {
        Console.WriteLine($"[{ModuleName}][match] {template} <<= {obj}");
        return null;
    }

    #endregion

    #region Substitute

    public SemanticObject? Substitute(SemanticObject obj, Dictionary<string, SemanticObject> vars,
        ModuleContext context)
    {
        Console.WriteLine($"[{ModuleName}][substitute] {obj} <<- {new Structure("", vars)}");
        return null;
    }

    #endregion


    public string ModuleName { get; init; }
    public List<SemanticObject> Reducers { get; init; }
}