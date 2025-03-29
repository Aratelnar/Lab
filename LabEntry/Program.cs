using System.Diagnostics;
using System.Reflection;
using AltLang;
using AltLang.Domain.Semantic;
using AltLang.Domain.Semantic.Explicit;
using AltLang.Parser.Semantic;
using Lang.Domain;
using Lang.Domain.TypeSystem;
using Lang.Parser;
using Lang.RuleReader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

        var modules = new List<Module>();
        foreach (var headerFile in Directory.EnumerateFiles("modules", "*.alth", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(headerFile);
            var text = File.ReadAllText(headerFile);
            var module = ModuleReader.ReadModule(name, text);
            modules.Add(module);
        }

        modules.Add(new Module("MainProgram", modules.Select(i => i.Name).ToList(), AutomataBuilder.EmptyAutomata));
        var (reducers, finalAutomata) = BuildReducers(modules);

        var keywords = finalAutomata.KnownTokens.Where(t => t is Terminal {Type: TerminalType.Keyword}).Cast<Terminal>()
            .Select(i => i.Token);
        // var input = File.ReadAllText("test.alt");
        // var text = Lexer.FromKeywords(keywords).Read(input).ToList();

        // var value = finalAutomata.Read(text);
        // Console.WriteLine($"read text {sw.ElapsedMilliseconds}");
        // Console.WriteLine(value);
        var context = GetContext(reducers);
        // Console.WriteLine(context.Reduce(value!));
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
            // Console.WriteLine(e.Message);
            return null;
        }
    }

    private static (List<ILangModule> reducers, SemanticAutomata finalAutomata) BuildReducers(List<Module> modules)
    {
        var sw = new Stopwatch();
        var automatas = new Dictionary<string, SemanticAutomata>();
        var reducers = new List<ILangModule>();
        foreach (var module in TopSort(modules.ToDictionary(m => m.Name)))
        {
            sw.Restart();
            var automata = AutomataBuilder.MergeAutomata(module.Imports.Select(automatas.GetValueOrDefault)
                .Append(module.Automata).ToArray()!);
            Console.WriteLine($"  [{module.Name}] automata merged in {sw.ElapsedMilliseconds}ms");
            automatas[module.Name] = automata;
            reducers.Add(CreateReducer(module.Name, automata));
            Console.WriteLine($"  [{module.Name}] reducer created in {sw.ElapsedMilliseconds}ms");
            sw.Stop();
            Console.WriteLine($"    [{module.Name}] has been built in {sw.ElapsedMilliseconds}ms");
        }

        return (reducers, automatas[modules.Last().Name]);
    }

    private static ILangModule CreateReducer(string moduleName, SemanticAutomata automata)
    {
        var path = Directory.EnumerateFiles("modules/", $"{moduleName}.alth").FirstOrDefault("");
        if (moduleName == "Core") return new CoreModule();
        // if (moduleName == "Core.Definitions") return new CoreDefinitionsModule();
        // if (moduleName == "Core.Numbers") return new CoreNumbersModule();
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
                // .Where(a => !a.IsDynamic)
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

        return new AltModule("MainProgram", new Structure("Tuple", []));
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

    private static ModuleContext GetContext(List<ILangModule> modules)
    {
        var context = new ModuleContext();
        foreach (var module in modules) module.Register(context);
        return context;
    }
}