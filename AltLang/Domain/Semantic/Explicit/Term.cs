using System.Collections.Immutable;
using Lang.Util;
using static AltLang.Domain.Constructor;

namespace AltLang.Domain.Semantic.Explicit;

using SWord = Semantic.Word;

public abstract record Term;

#region Core

public record Word(string Name) : Term;

public record Function(Term Type, Term Result) : Term;

public record Application(Term Function, Term Argument) : Term;

public record Property(Term Object, string Name) : Term;

public record Or(Term Left, Term Right) : Term;

public record And(Term Left, Term Right) : Term;

public record StructureTemplate(string Name, Dictionary<string, Term> Children) : Term;

public record WordTemplate(string Name) : Term;

public record NameTemplate(string Name) : Term;

public record AnyTemplate : Term;

public record Match(Term Argument, List<Function> Cases) : Term;

public record Unknown(string Name, Dictionary<string, Term> Children) : Term;

#endregion

#region RecDef

public record Rec(string Name, Term Result) : Term;

public record Let(Term Var, Term Argument, Term Result) : Term;

#endregion

#region List

public record ListSeq(Term Head, Term Tail) : Term;

public record ListEnd : Term;

#endregion

#region Numbers

public record Number(int Value) : Term;

#endregion

public static class TermExtensions
{
    public static Term ToTerm(this SemanticObject obj) => obj switch
    {
        SWord w => new Word(w.Name),
        Structure {Name: "Function"} s => new Function(s["template"].ToTerm(), s["body"].ToTerm()),
        Structure {Name: "Application"} s => new Application(s["function"].ToTerm(), s["argument"].ToTerm()),
        Structure {Name: "Property"} s => new Property(s["object"].ToTerm(), (s["property"] as SWord)!.Name),
        Structure {Name: "Or"} s => new Or(s["left"].ToTerm(), s["right"].ToTerm()),
        Structure {Name: "And"} s => new And(s["left"].ToTerm(), s["right"].ToTerm()),
        Structure {Name: "Match"} s => new Match(s["argument"].ToTerm(),
            (s["cases"] as Structure)!.Children.Values.Select(ToTerm).Cast<Function>().ToList()),
        Structure {Name: "Rec"} s => new Rec((s["self"] as SWord)!.Name, s["body"].ToTerm()),
        Structure {Name: "Template"} s => s.ToTemplateTerm(),
        Structure {Name: "ListEnd"} => new ListEnd(),
        Structure {Name: "ListSeq"} s => new ListSeq(s["head"].ToTerm(), s["tail"].ToTerm()),
        Structure {Name: "LetDefine"} s => new Let(s["template"].ToTerm(), s["argument"].ToTerm(), s["body"].ToTerm()),
        Structure {Name: "Number"} s => new Number(int.Parse((s["value"] as SWord).Name)),
        Structure {Name: "Name"} s => new NameTemplate(s["pattern"].GetName()),
        Structure s => new Unknown(s.Name, s.Children.Map(ToTerm))
    };

    private static Term ToTemplateTerm(this Structure obj) => obj["pattern"] switch
    {
        SWord w => new WordTemplate(w.Name),
        Structure s => new StructureTemplate(s.Name, s.Children.Map(ToTerm))
    };

    public static SemanticObject ToObject(this Term term) => term switch
    {
        And(var left, var right) => And(left.ToObject(), right.ToObject()),
        Application(var function, var argument) => Application(function.ToObject(), argument.ToObject()),
        Function(var type, var result) => Function(type.ToObject(), result.ToObject()),
        Match(var argument, var functions) => Match(argument.ToObject(), Object(functions.Select(ToObject))),
        Or (var left, var right) => Or(left.ToObject(), right.ToObject()),
        Property(var obj, var name) => Property(obj.ToObject(), Word(name)),
        Rec(var name, var result) => Rec(Word(name), result.ToObject()),
        StructureTemplate(var name, var children) => Constructor.Template(new Structure(name,
            children.Map(ToObject))),
        Word(var name) => Word(name),
        WordTemplate(var name) => Constructor.Template(Word(name)),
        NameTemplate(var name) => Name(Word(name)),
        ListEnd => new Structure("ListEnd", []),
        ListSeq(var head, var tail) => StructureBuild.New("ListSeq")
            .Child("head", head.ToObject())
            .Child("tail", tail.ToObject()),
        Let(var var, var argument, var result) => StructureBuild.New("LetDefine")
            .Child("template", var.ToObject())
            .Child("argument", argument.ToObject())
            .Child("body", result.ToObject()),
        Unknown(var name, var children) => new Structure(name, children.Map(ToObject)),
    };

    public static string Print(this Term term) => term switch
    {
        And(var left, var right) => $"({left.Print()} & {right.Print()})",
        Application(var function, var argument) => $"({function.Print()} {argument.Print()})",
        Function(var type, var result) => $"({type.Print()} => {result.Print()})",
        Match(var argument, var functions) =>
            $"match {argument.Print()} {{{string.Join(", ", functions.Select(Print))}}}",
        Or (var left, var right) => $"({left.Print()} | {right.Print()})",
        Property(var obj, var name) => $"({obj.Print()}.{name})",
        Rec(var name, var result) => $"(rec {name}. {result.Print()})",
        WordTemplate(var name) => $"#{name}",
        AnyTemplate => "any",
        StructureTemplate(var name, var children) =>
            $"(#{name}{{{string.Join(", ", children.Select(p => $"{p.Key}: {p.Value.Print()}"))}}})",
        NameTemplate(var name) => $"@{name}",
        Word(var name) => name,
        ListEnd => "[]",
        ListSeq list => PrintList(list),
        Number n => n.Value.ToString(),
        Let(var var, var argument, var result) => $"let {var.Print()} = {argument.Print()} in {result.Print()}",
        Unknown(var name, var children) =>
            $"{name}{{{string.Join(", ", children.Select(p => $"{p.Key}: {p.Value.Print()}"))}}}",
    };

    private static string PrintList(Term term)
    {
        var list = new List<string>();
        while (term is not ListEnd)
        {
            if (term is not ListSeq(var a, var b)) return $"({string.Join(" :: ", list.Append(term.Print()))})";
            list.Add(a.Print());
            term = b;
        }

        return $"[{string.Join(", ", list)}]";
    }

    public static ImmutableHashSet<WordTemplate> TemplateVariables(this Term term) => term switch
    {
        And(var l, var r) => l.TemplateVariables().Union(r.TemplateVariables()),
        Application(var f, var a) => f.TemplateVariables().Union(a.TemplateVariables()),
        Function(var t, var r) => r.TemplateVariables(),
        Or(var l, var r) => l.TemplateVariables().Union(r.TemplateVariables()),
        Rec(_, var r) => r.TemplateVariables(),
        StructureTemplate(_, var children) => children.Values.SelectMany(p => p.TemplateVariables())
            .ToImmutableHashSet(),
        WordTemplate wordTemplate => [wordTemplate],
        _ => []
    };
}