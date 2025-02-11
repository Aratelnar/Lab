using LabEntry.domain;

namespace AltLang.Domain.Semantic;

public static class Constructor
{
    public static Structure TemplateRec(SemanticObject pattern) =>
        new("TemplateRec", new Dictionary<string, SemanticObject> {["pattern"] = pattern});

    public static Structure Template(SemanticObject pattern) =>
        new("Template", new Dictionary<string, SemanticObject> {["pattern"] = pattern});

    public static Structure And(SemanticObject left, SemanticObject right) =>
        new("And", new Dictionary<string, SemanticObject> {["left"] = left, ["right"] = right});

    public static Structure Application(SemanticObject function, SemanticObject argument) =>
        new("Application", new Dictionary<string, SemanticObject> {["function"] = function, ["argument"] = argument});

    public static Structure Property(SemanticObject obj, SemanticObject property) =>
        new("Property", new Dictionary<string, SemanticObject> {["object"] = obj, ["property"] = property});

    public static Structure Function(SemanticObject template, SemanticObject body) =>
        new("Function", new Dictionary<string, SemanticObject> {["template"] = template, ["body"] = body});

    public static Structure Match(SemanticObject argument, SemanticObject cases) =>
        new("Match", new Dictionary<string, SemanticObject> {["argument"] = argument, ["cases"] = cases});

    public static Structure RecFunction(SemanticObject self, SemanticObject body) =>
        new("RecFunction", new Dictionary<string, SemanticObject> {["self"] = self, ["body"] = body});

    public static Structure Or(SemanticObject left, SemanticObject right) =>
        new("Or", new Dictionary<string, SemanticObject> {["left"] = left, ["right"] = right});

    public static Word Word(string name) => new(name);
    public static Word Any => new("");

    public static Structure ApplyToChildren(Structure str, Func<SemanticObject, SemanticObject> map) =>
        str with {Children = str.Children.ToDictionary(p => p.Key, p => map(p.Value))};

    public static TypeObject AsTemplate(SemanticObject template)
    {
        SemanticObject RecTemplated(SemanticObject t) => t switch
        {
            Structure s => Template(ApplyToChildren(s, RecTemplated)),
            Word w => Template(w),
        };
        return TypeObject.FromSemanticObject(RecTemplated(template));
    }

    public static string Print(SemanticObject? value)
    {
        switch (value)
        {
            case Word w:
                return w.Name;
            case Structure s:
            {
                var paramsRaw = string.Join(",", s.Children.Select(p => $"{p.Key}: {Print(p.Value)}"));
                switch (s.Name)
                {
                    case "":
                        return $"{{{paramsRaw}}}";
                    case "Tuple":
                        return $"({paramsRaw})";
                    case "Template":
                        return $"#{Print(s["pattern"])}";
                    case "Property":
                        return $"({Print(s["object"])}).{Print(s["property"])}";
                    case "Function":
                        return $"({Print(s["template"])}) => ({Print(s["body"])})";
                    case "RecFunction":
                        return $"rec {Print(s["self"])} => ({Print(s["body"])})";
                    case "And":
                        return $"({Print(s["left"])}) & ({Print(s["right"])})";
                    case "Or":
                        return $"({Print(s["left"])}) | ({Print(s["right"])})";
                    case "Match":
                        return $"match ({Print(s["argument"])}) " +
                               $"{{ {string.Join(", ", (s["cases"] as Structure)!.Children.Select(c => Print(c.Value)))} }}";
                    case "Application":
                        return $"({Print(s["function"])}) ({Print(s["argument"])})";
                    case "ListEnd":
                    case "ListSeq":
                        var list = new List<string>();
                        var ss = s;
                        while (ss.Name == "ListSeq")
                        {
                            list.Add(Print(ss["head"]));
                            if (ss["tail"] is not Structure {Name: "ListSeq"})
                            {
                                if (ss["tail"] is not Structure {Name: "ListEnd"})
                                    goto def;
                                break;
                            }

                            ss = (ss["tail"] as Structure)!;
                        }

                        return $"[{string.Join(",", list)}]";
                    default:
                        def:
                        return $"{s.Name}{{{paramsRaw}}}";
                }
            }
            default:
                return "undefined";
        }
    }
}

public record StructureBuild(Structure Structure)
{
    public static implicit operator Structure(StructureBuild structure) => structure.Structure;
    public StructureBuild Child(string key, SemanticObject child)
    {
        Structure.Children.Add(key, child);
        return this;
    }

    public static StructureBuild New(string name) => new(new Structure(name, new Dictionary<string, SemanticObject>()));
}