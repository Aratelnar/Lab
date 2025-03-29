using System.Runtime.CompilerServices;
using AltLang.Domain;
using AltLang.Domain.Semantic;
using AltLang.Domain.Semantic.Explicit;
using AltLang.Template;
using Lang.Domain.TypeSystem;
using Lang.Util;
using Word = AltLang.Domain.Semantic.Word;

namespace AltLang;

using Assump = Dictionary<string, Term>;

public interface ILangModule
{
    public string ModuleName { get; }
    public void Register(ModuleContext context);
    public SemanticObject? Reduce(SemanticObject obj, ModuleContext context);

    public Dictionary<string, SemanticObject>?
        Match(SemanticObject template, SemanticObject obj, ModuleContext context);

    public (Term, Assump)? Infer(SemanticObject obj, Assump assumptions, ModuleContext context);

    public SemanticObject? Substitute(SemanticObject obj, Dictionary<string, SemanticObject> vars,
        ModuleContext context);
}

public class ModuleContext
{
    public readonly TemplateDictionary<ILangModule> ModuleToReduce = new();
    public readonly TemplateDictionary<ILangModule> ModuleToMatch = new();
    public readonly TemplateDictionary<ILangModule> ModuleToInfer = new();
    public readonly TemplateDictionary<ILangModule> ModuleToSubstitute = new();

    public readonly TypeResolver TypeResolver = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log(string message)
    {
        // Console.WriteLine(message);
    }

    public SemanticObject Reduce(SemanticObject obj) =>
        ModuleToReduce.GetValueOrDefault(obj)?.Reduce(obj, this) ?? obj;

    public Dictionary<string, SemanticObject>? Match(SemanticObject tmpl, SemanticObject arg) =>
        ModuleToMatch.GetValueOrDefault(tmpl)?.Match(tmpl, arg, this);

    public (Term, Assump)? Infer(SemanticObject obj, Assump assumptions) =>
        ModuleToInfer.GetValueOrDefault(obj)?.Infer(obj, assumptions, this) ?? DefaultInfer(obj, assumptions);

    public SemanticObject Substitute(SemanticObject obj, Dictionary<string, SemanticObject> vars) => vars.Count == 0
        ? obj
        : ModuleToSubstitute.GetValueOrDefault(obj)?.Substitute(obj, vars, this) ?? DefaultSubstitute(obj, vars);

    private SemanticObject DefaultSubstitute(SemanticObject obj, Dictionary<string, SemanticObject> vars) => obj switch
    {
        Structure s => Constructor.ApplyToChildren(s, c => Substitute(c, vars)),
        Word w => vars.GetValueOrDefault(w.Name, w)
    };

    private (Term, Assump)? DefaultInfer(SemanticObject obj, Assump assumptions) => obj switch
    {
        Structure s => (new StructureTemplate(
            s.Name,
            s.Children.Map(p => Infer(p, assumptions)!.Value.Item1)), []),
        Word {Name: "_"} => (new AnyTemplate(), []),
        Word w => (assumptions.GetValueOrDefault(w.Name) ?? throw new TypeException(), [])
    };
}