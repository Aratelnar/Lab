using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AltLang;
using AltLang.Domain;
using AltLang.Domain.Semantic;
using AltLang.Domain.Semantic.Explicit;
using static AltLang.Domain.Constructor;
using Word = AltLang.Domain.Semantic.Word;

namespace LabEntry.core;

public class CoreMacroModule : ILangModule
{
    public string ModuleName => "Core.Macro";

    public void Register(ModuleContext context)
    {
        context.ModuleToReduce.Register(AsTemplate(Application(StructureBuild.New("Rho").Child("expr", Any), Any)),
            this);
    }

    public SemanticObject? Reduce(SemanticObject obj, ModuleContext context)
    {
        var func = ((obj as Structure)!["function"] as Structure)!["expr"];
        var arg = (obj as Structure)!["argument"];
        arg = context.Reduce(arg);

        return arg switch
        {
            Structure s => ApplyToChildren(s, ch => Application(func, ch)),
            Word w => w,
        };
    }

    public Dictionary<string, SemanticObject>? Match(SemanticObject template, SemanticObject obj, ModuleContext context)
    {
        return null;
    }

    public (Term, Dictionary<string, Term>)? Infer(SemanticObject obj, Dictionary<string, Term> assumptions,
        ModuleContext context)
    {
        return null;
    }

    public SemanticObject? Substitute(SemanticObject obj, Dictionary<string, SemanticObject> vars,
        ModuleContext context)
    {
        return null;
    }
}