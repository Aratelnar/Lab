using AltLang.Domain.Semantic;
using AltLang.Template;
using LabEntry.domain;

namespace AltLang;

public interface ILangModule
{
    public string ModuleName { get; }
    public void Register(ModuleContext context);
    public SemanticObject? Reduce(SemanticObject obj, ModuleContext context);

    public Dictionary<string, SemanticObject>?
        Match(SemanticObject template, SemanticObject obj, ModuleContext context);

    public SemanticObject? Substitute(SemanticObject obj, Dictionary<string, SemanticObject> vars,
        ModuleContext context);
}

public class ModuleContext
{
    public readonly TemplateDictionary<ILangModule> ModuleToReduce = new();
    public readonly TemplateDictionary<ILangModule> ModuleToMatch = new();
    public readonly TemplateDictionary<ILangModule> ModuleToSubstitute = new();

    public SemanticObject Reduce(SemanticObject obj) =>
        ModuleToReduce.GetValueOrDefault(obj)?.Reduce(obj, this) ?? obj;

    public Dictionary<string, SemanticObject>? Match(SemanticObject tmpl, SemanticObject arg) =>
        ModuleToMatch.GetValueOrDefault(tmpl)?.Match(tmpl, arg, this);

    public SemanticObject Substitute(SemanticObject obj, Dictionary<string, SemanticObject> vars) =>
        ModuleToSubstitute.GetValueOrDefault(obj)?.Substitute(obj, vars, this) ?? DefaultSubstitute(obj, vars);

    private SemanticObject DefaultSubstitute(SemanticObject obj, Dictionary<string, SemanticObject> vars) => obj switch
    {
        Structure s => Constructor.ApplyToChildren(s, c => Substitute(c, vars)),
        Word w => vars.GetValueOrDefault(w.Name, w)
    };
}