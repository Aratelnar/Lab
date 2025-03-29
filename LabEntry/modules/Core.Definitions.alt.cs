using AltLang;
using LabEntry.domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AltLang.Domain;
using AltLang.Domain.Semantic;
using AltLang.Domain.Semantic.Explicit;
using Lang.Util;
using static AltLang.Domain.Constructor;
using Word = AltLang.Domain.Semantic.Word;

namespace LabEntry.core;

public class CoreDefinitionsModule : ILangModule
{
    public string ModuleName => "Core.Definitions";

    public void Register(ModuleContext context)
    {
        var letDefine = StructureBuild.New("LetDefine")
            .Child("body", Any)
            .Child("argument", Any)
            .Child("template", Any);

        context.ModuleToReduce.Register(AsTemplate(StructureBuild.New("Calc").Child("expr", Any)), this);
        context.ModuleToReduce.Register(AsTemplate(letDefine), this);

        context.ModuleToSubstitute.Register(AsTemplate(letDefine), this);
    }

    public SemanticObject? Reduce(SemanticObject obj, ModuleContext context)
    {
        context.Log($"[{ModuleName}][reduce] {obj.ToTerm().Print()}");
        return obj switch
        {
            Structure {Name: "Calc", Children: var c} => context.Reduce(c["expr"]) switch
            {
                Structure s => ApplyToChildren(s,
                    c1 => context.Reduce(new Structure("Calc",
                        new Dictionary<string, SemanticObject> {["expr"] = c1}))),
                var a => a
            },
            Structure {Name: "LetDefine"} s => context.Reduce(Application(Function(s["template"], s["body"]),
                s["argument"])),
            _ => null
        };
    }

    public Dictionary<string, SemanticObject>?
        Match(SemanticObject template, SemanticObject obj, ModuleContext context) => null;

    public (Term, Dictionary<string, Term>)? Infer(SemanticObject obj, Dictionary<string, Term> assumptions,
        ModuleContext context) => null;

    public SemanticObject? Substitute(SemanticObject obj, Dictionary<string, SemanticObject> vars,
        ModuleContext context)
    {
        context.Log($"[{ModuleName}][substitute] {obj.ToTerm().Print()} <<- {new Unknown("", vars.Map(p => p.ToTerm())).Print()}");
        switch (obj)
        {
            case Structure {Name: "LetDefine"} s:
                var escaped = GetEscapedVars(s["template"]);
                var allowedVars = vars.Where(v => !escaped.Contains(v.Key)).ToDictionary();
                return ApplyToChildren(s, c => context.Substitute(c, allowedVars));
            default:
                return null;
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
}