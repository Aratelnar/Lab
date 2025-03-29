using AltLang;
using AltLang.Domain.Semantic;
using AltLang.Domain.Semantic.Explicit;
using AltLang.Parser.Semantic;
using LabEntry.Compiler;
using Lang.Domain;
using Lang.Domain.TypeSystem;
using Lang.Parser;
using Lexer = Lang.Lexer.Lexer;
using Module = AltLang.Domain.Semantic.Module;

internal class Program
{
    public static void Main(string[] args)
    {
        // var stream = new AntlrFileStream("test.alth");
        // var lexer = new HeaderLangLexer(stream);
        // var tokenStream = new CommonTokenStream(lexer);
        // var parser = new HeaderLangParser(tokenStream);
        //
        // var el = parser.prog().nodes;
        // foreach (var (key, value) in el)
        // {
        //     Console.WriteLine($"{key}: {value}");
        // }

        var modules = Compiler.CompileFromSource("modules");

        modules.Add("", new Module("MainProgram", modules.Values.Select(i => i.Name).ToList(), AutomataBuilder.EmptyAutomata));
        var (reducers, finalAutomata) = Compiler.BuildReducers(modules);

        var keywords = finalAutomata.KnownTokens.Where(t => t is Terminal {Type: TerminalType.Keyword}).Cast<Terminal>()
            .Select(i => i.Token);

        var context = GetContext(reducers);
        var lexer = Lexer.FromKeywords(keywords);
        var currentModule = reducers.First(r => r.ModuleName == "MainProgram") as AltModule;
        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine() ?? "";
            var text = lexer.Read(line).ToList();
            var value = finalAutomata.Read(text);
            if (value is Structure {Name: "Reduce" or "Define"})
                currentModule.AddReducer(value, context);

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(value?.ToTerm().Print());
            if (value != null)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($":t {InferType(value, context)?.Print()}");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                var reduce = context.Reduce(value);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(reduce.ToTerm().Print());
            }

            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }

    private static Term? InferType(SemanticObject reduce, ModuleContext context)
    {
        try
        {
            var (type, _) = context.Infer(reduce, []) ?? throw new TypeException();
            return context.TypeResolver.Subst1(type);
        }
        catch(Exception e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }

    private static void CountDensity(SemanticAutomata automata)
    {
        var width = automata.Actions.Keys.Select(k => k.Item2).Distinct().Count();
        var height = automata.Actions.Keys.Select(k => k.Item1).Distinct().Count();
        Console.WriteLine($"width: {width}, height: {height}");
        Console.WriteLine($"space count: {width * height}");
        Console.WriteLine($"actions count: {automata.Actions.Count}");
        Console.WriteLine($"density: {automata.Actions.Count * 1.0 / (width * height)}");
    }

    private static ModuleContext GetContext(List<ILangModule> modules)
    {
        var context = new ModuleContext();
        foreach (var module in modules) module.Register(context);
        return context;
    }
}