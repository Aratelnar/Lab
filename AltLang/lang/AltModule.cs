using AltLang.Domain;
using AltLang.Domain.Semantic;
using AltLang.Domain.Semantic.Explicit;
using AltLang.Template;
using Word = AltLang.Domain.Semantic.Word;

namespace AltLang;

using Assump = Dictionary<string, Term>;

public record AltModule : ILangModule
{
    public AltModule(string ModuleName, SemanticObject Reducers)
    {
        this.ModuleName = ModuleName;
        this.Reducers = (Reducers as Structure)?.Children.Values.ToList() ?? [];
    }

    private TemplateDictionary<SemanticObject> reduceMap = new();

    #region Reduce

    public void Register(ModuleContext context)
    {
        foreach (var reducer in Reducers)
            HandleReducer(reducer, context);
    }

    public void AddReducer(SemanticObject obj, ModuleContext context)
    {
        if (!HandleReducer(obj, context)) return;
        Reducers.Add(obj);
    }

    private bool HandleReducer(SemanticObject obj, ModuleContext context)
    {
        switch (obj)
        {
            case Structure {Name: "Reduce", Children: { } cc}:
                if (!cc.ToDictionary().TryGetValue("rule", out var rule)) return false;
                var term = rule.ToTerm();
                if (term is not Function {Type: StructureTemplate st}) return false;
                context.ModuleToReduce.Register(st, this);
                reduceMap.Register(st, obj);
                break;
            case Structure {Name: "Define", Children: { } cc2}:
                if (!cc2.ToDictionary().TryGetValue("name", out var name) || name is not Word w) return false;
                context.ModuleToReduce.RegisterWord(w, this);
                context.ModuleToInfer.RegisterWord(w, this);
                reduceMap.RegisterWord(w, obj);
                break;

            case Structure {Name: "TypeDef", Children: { } cc3}:
                if (!cc3.ToDictionary().TryGetValue("term", out var term2)) return false;
                var ter = term2.ToTerm();
                context.ModuleToInfer.Register(ter, this);
                reduceMap.Register(ter, obj);
                break;
        }

        return true;
    }

    public SemanticObject? Reduce(SemanticObject obj, ModuleContext context)
    {
        context.Log($"[{ModuleName}][reduce] {obj.ToTerm().Print()}");
        var reducer = reduceMap.GetValueOrDefault(obj);
        switch (reducer)
        {
            case Structure {Name: "Reduce", Children: { } cc}:
                if (!cc.ToDictionary().TryGetValue("rule", out var rule)) return null;
                var result = TryApplyApplication(rule, obj, context);
                if (result is null) return null;
                return context.Reduce(result);
            case Structure {Name: "Define", Children: { } cc2}:
                if (!cc2.ToDictionary().TryGetValue("name", out var name) || name is not Word w) return null;
                if (!cc2.ToDictionary().TryGetValue("value", out var val)) return null;
                if (obj is Word w2 && w2.Name == w.Name) return val;
                break;
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
        context.Log($"[{ModuleName}][match] {template.ToTerm().Print()} <<= {obj.ToTerm().Print()}");
        return null;
    }

    public (Term, Assump)? Infer(SemanticObject obj, Assump assumptions, ModuleContext context)
    {
        var reducer = reduceMap.GetValueOrDefault(obj);
        switch (reducer)
        {
            case Structure {Name: "Define", Children: { } cc2}:
                if (!cc2.ToDictionary().TryGetValue("name", out var name) || name is not Word w) return null;
                if (!cc2.ToDictionary().TryGetValue("value", out var val)) return null;
                if (obj is Word w2 && w2.Name == w.Name) return context.Infer(val, assumptions);
                break;
        }

        return null;
    }

    #endregion

    #region Substitute

    public SemanticObject? Substitute(SemanticObject obj, Dictionary<string, SemanticObject> vars,
        ModuleContext context)
    {
        context.Log($"[{ModuleName}][substitute] {obj.ToTerm().Print()} <<- {new Structure("", vars)}");
        return null;
    }

    #endregion


    public string ModuleName { get; init; }
    public List<SemanticObject> Reducers { get; init; }
}