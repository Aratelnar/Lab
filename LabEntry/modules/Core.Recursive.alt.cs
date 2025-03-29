using AltLang;
using LabEntry.domain;
using static AltLang.Domain.Constructor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AltLang.Domain.Semantic;
using AltLang.Domain.Semantic.Explicit;
using Lang.Domain.TypeSystem;
using Lang.Util;
using Function = LabEntry.domain.Function;
using Structure = AltLang.Domain.Semantic.Structure;
using Word = AltLang.Domain.Semantic.Word;

namespace LabEntry.core;

using Assump = Dictionary<string, Term>;

public class CoreRecursiveModule : ILangModule
{
    public string ModuleName => "Core.Recursive";

    public void Register(ModuleContext context)
    {
        context.ModuleToReduce.Register(AsTemplate(Rec(Any, Any)), this);
        context.ModuleToSubstitute.Register(AsTemplate(Rec(Any, Any)), this);
        context.ModuleToInfer.Register(AsTemplate(Rec(Any, Any)), this);
    }

    public SemanticObject? Reduce(SemanticObject obj, ModuleContext context)
    {
        return obj switch
        {
            Structure {Name: "Rec"} s => context.Reduce(
                Application(Function(Template(s["self"]), s["body"]), s)
            ),
            _ => null
        };
    }

    public Dictionary<string, SemanticObject>?
        Match(SemanticObject template, SemanticObject obj, ModuleContext context) => null;

    public (Term, Assump)? Infer(SemanticObject obj, Assump assumptions,
        ModuleContext context)
    {
        if (obj is not Structure {Name: "Rec"} r) return null;
        var selfType = context.TypeResolver.NewVar();
        var selfName = (r["self"] as Word)!.Name;
        var assump = new Assump {[selfName] = selfType};
        var (bodyType, _) = context.Infer(r["body"], LinqExtensions.Combine(assump, assumptions)) ??
                            throw new TypeException();
        context.TypeResolver.Unify(selfType, bodyType);
        return (selfType, []);
    }

    public SemanticObject? Substitute(SemanticObject obj, Dictionary<string, SemanticObject> vars,
        ModuleContext context)
    {
        context.Log($"[{ModuleName}][substitute] {obj.ToTerm().Print()} <<- {new Unknown("", vars.Map(p => p.ToTerm())).Print()}");
        return obj switch
        {
            Structure {Name: "Rec"} s => Rec(s["self"], context.Substitute(s["body"], vars)),
            _ => null
        };
    }
}