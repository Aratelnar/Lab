using AltLang;
using LabEntry.domain;
using static AltLang.Domain.Semantic.Constructor;
using System;
using System.Collections.Generic;
using System.Linq;
using AltLang.Domain.Semantic;
using static AltLang.Domain.Semantic.Constructor;

namespace LabEntry.core;

public class CoreRecursiveModule : ILangModule
{
    public string ModuleName => "Core.Recursive";

    public void Register(ModuleContext context)
    {
        context.ModuleToReduce.Register(AsTemplate(Application(RecFunction(Any, Any), Any)), this);
        context.ModuleToSubstitute.Register(AsTemplate(RecFunction(Any, Any)), this);
    }

    public SemanticObject? Reduce(SemanticObject obj, ModuleContext context)
    {
        return obj switch
        {
            Structure {Name: "Application"} s => ReduceApplication(s["function"], s["argument"], context),
            _ => null
        };
    }

    private SemanticObject? ReduceApplication(SemanticObject function, SemanticObject argument, ModuleContext context)
    {
        return function switch
        {
            Structure {Name: "RecFunction"} s => context.Reduce(
                Application(Application(Function(s["self"], s["body"]), s), argument)),
            _ => null,
        };
    }

    public Dictionary<string, SemanticObject>?
        Match(SemanticObject template, SemanticObject obj, ModuleContext context) => null;

    public SemanticObject? Substitute(SemanticObject obj, Dictionary<string, SemanticObject> vars,
        ModuleContext context)
    {
        Console.WriteLine($"[{ModuleName}][substitute] {Print(obj)} <<- {Print(new Structure("", vars))}");
        return obj switch
        {
            Structure {Name: "RecFunction"} s => RecFunction(s["self"], context.Substitute(s["body"], vars)),
            _ => null
        };
    }
}