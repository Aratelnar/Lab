using AltLang;
using LabEntry.domain;
using System;
using System.Collections.Generic;
using System.Linq;
using AltLang.Domain;
using AltLang.Domain.Semantic;
using AltLang.Domain.Semantic.Explicit;
using static AltLang.Domain.Constructor;
using Structure = AltLang.Domain.Semantic.Structure;
using Word = AltLang.Domain.Semantic.Word;

namespace LabEntry.core;

public class CoreNumbersModule : ILangModule
{
    public string ModuleName => "Core.Numbers";

    public void Register(ModuleContext context)
    {
        SemanticObject Op(string name) => StructureBuild.New(name).Child("left", Any).Child("right", Any);
        context.ModuleToReduce.Register(AsTemplate(Op("Plus")), this);
        context.ModuleToReduce.Register(AsTemplate(Op("Star")), this);
        context.ModuleToReduce.Register(AsTemplate(Op("Minus")), this);
        context.ModuleToReduce.Register(AsTemplate(Op("Slash")), this);

        context.ModuleToInfer.Register(AsTemplate(StructureBuild.New("Number").Child("value", Any)), this);
    }

    public SemanticObject? Reduce(SemanticObject obj, ModuleContext context)
    {
        context.Log($"[{ModuleName}][reduce] {obj.ToTerm().Print()}");
        return obj switch
        {
            Structure {Name: var name} s => ReduceOperation(name, s["left"], s["right"], context),
            _ => null
        };
    }

    private SemanticObject? ReduceOperation(string operation, SemanticObject left, SemanticObject right,
        ModuleContext context)
    {
        var leftInt = ExtractNumber(left, context);
        var rightInt = ExtractNumber(right, context);
        return operation switch
        {
            "Plus" => StructureBuild.New("Number").Child("value", Word((leftInt + rightInt).ToString())),
            "Star" => StructureBuild.New("Number").Child("value", Word((leftInt * rightInt).ToString())),
            "Minus" => StructureBuild.New("Number").Child("value", Word((leftInt - rightInt).ToString())),
            "Slash" => StructureBuild.New("Number").Child("value", Word((leftInt / rightInt).ToString())),
            _ => (Structure?) null
        };
    }

    private int ExtractNumber(SemanticObject obj, ModuleContext context)
    {
        var reduced = false;
        int number;
        while (obj is not Structure {Name: "Number", Children: { } c}
               || !c.ToDictionary().TryGetValue("value", out var line)
               || line is not Word {Name: var w}
               || !int.TryParse(w, out number))
        {
            if (reduced) throw new Exception($"Not a number: {obj}");
            obj = context.Reduce(obj);
            reduced = true;
        }

        return number;
    }

    public Dictionary<string, SemanticObject>? Match(SemanticObject template, SemanticObject obj, ModuleContext context)
    {
        return null;
    }

    public (Term, Dictionary<string, Term>)? Infer(SemanticObject obj, Dictionary<string, Term> assumptions,
        ModuleContext context) => (new StructureTemplate("Number", []), []);

    public SemanticObject? Substitute(SemanticObject obj, Dictionary<string, SemanticObject> vars,
        ModuleContext context)
    {
        return null;
    }
}