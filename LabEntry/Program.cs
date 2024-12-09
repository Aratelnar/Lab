using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using AltLang;
using Antlr4.Runtime;
using LabEntry.domain;
using Lang.Domain;
using Lang.Domain.Semantic;
using Lang.GrammarTransform;
using Lang.Parser;
using Lang.Parser.Semantic;
using Lang.RuleReader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CSharp;
using Lexer = Lang.Lexer.Lexer;
using Module = AltLang.Domain.Semantic.Module;
using Token = Lang.Domain.Token;

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

        var axiom = new NonTerminal("Expr");
        var sw = new Stopwatch();
        var modules = new List<Module>();
        foreach (var headerFile in Directory.EnumerateFiles("modules", "*.alth", SearchOption.AllDirectories))
        {
            sw.Start();
            var name = Path.GetFileNameWithoutExtension(headerFile);
            var module = ModuleReader.ReadModule(name, File.ReadAllText(headerFile));
            Console.WriteLine($"Read module {name} in {sw.ElapsedMilliseconds}ms");
            sw.Reset();
            modules.Add(module);
        }

        modules.Add(new Module("MainProgram", modules.Select(i => i.Name).ToList(), AutomataBuilder.EmptyAutomata));
        var (reducers, finalAutomata) = BuildReducers(modules);

        var input = File.ReadAllText("test.alt");
        var keywords = finalAutomata.KnownTokens.Where(t => t is Terminal {Type: TerminalType.Keyword}).Cast<Terminal>()
            .Select(i => i.Token);
        var text = Lexer.FromKeywords(keywords).Read(input).ToList();

        var value = finalAutomata.Read(text);
        Console.WriteLine($"read text {sw.ElapsedMilliseconds}");
        Console.WriteLine(value);
        var red = Reduce(value!, reducers);
        Console.WriteLine(red);
    }

    private static (List<ILangModule> reducers, SemanticAutomata finalAutomata) BuildReducers(List<Module> modules)
    {
        var automatas = new Dictionary<string, SemanticAutomata>();
        var reducers = new List<ILangModule>();
        foreach (var module in TopSort(modules.ToDictionary(m => m.Name)))
        {
            var automata = AutomataBuilder.MergeAutomata(module.Imports.Select(automatas.GetValueOrDefault)
                .Append(module.Automata).ToArray()!);
            automatas[module.Name] = automata;
            reducers.Add(CreateReducer(module.Name, automata));
        }

        return (reducers, automatas[modules.Last().Name]);
    }

    private static ILangModule CreateReducer(string moduleName, SemanticAutomata automata)
    {
        var path = Directory.EnumerateFiles("modules/", $"{moduleName}.alth").FirstOrDefault("");
        if (File.Exists(path.Replace(".alth", ".alt")))
        {
            var rText = File.ReadAllText(path.Replace(".alth", ".alt"));
            var keywords = automata.KnownTokens
                .Where(t => t is Terminal {Type: TerminalType.Keyword})
                .Cast<Terminal>()
                .Select(t => t.Token);
            var tokens = Lexer.FromKeywords(keywords).Read(rText).ToList();
            var obj = automata.Read(tokens);
            return new AltModule(moduleName, obj!);
        }

        if (File.Exists(path.Replace(".alth", ".alt.cs")))
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(path.Replace(".alth", ".alt.cs")));
            var references = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic)
                .Select(a => a.Location)
                .Where(s => !string.IsNullOrEmpty(s))
                .Where(s => !s.Contains("xunit"))
                .Select(s => MetadataReference.CreateFromFile(s))
                .ToList();
            var compilation = CSharpCompilation.Create(Path.GetRandomFileName(), [syntaxTree],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using var ms = new MemoryStream();
            var emit = compilation.Emit(ms);
            ms.Seek(0, SeekOrigin.Begin);
            var assembly = Assembly.Load(ms.ToArray());
            var type = assembly.GetTypes().First(t => typeof(ILangModule).IsAssignableFrom(t));
            return (ILangModule) Activator.CreateInstance(type)!;
        }

        return new AltModule("MainProgram", new Structure("Tuple", new Dictionary<string, SemanticObject>()));
    }

    private static IEnumerable<Module> TopSort(Dictionary<string, Module> modules)
    {
        var state = new Dictionary<string, bool>();
        var stack = new Stack<string>();
        var deps = modules.Values.SelectMany(m => m.Imports).ToHashSet();
        var start = modules.Keys.Except(deps);
        foreach (var s in start) stack.Push(s);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (state.TryGetValue(node, out var value))
            {
                if (value) continue;
                yield return modules[node];
                state[node] = true;
                continue;
            }

            state[node] = false;
            stack.Push(node);
            foreach (var import in modules[node].Imports)
            {
                if (state.TryGetValue(import, out var stateValue))
                {
                    if (!stateValue) throw new Exception();
                    continue;
                }

                stack.Push(import);
            }
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

    private static SemanticObject Reduce(SemanticObject node, List<ILangModule> reducers)
    {
        SemanticObject ReduceInternal(SemanticObject n) =>
            reducers.Select(reducer => reducer.Reduce(n, ReduceInternal)).FirstOrDefault(i => i is not null) ?? n;

        return ReduceInternal(node);
    }
}