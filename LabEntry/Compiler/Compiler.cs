using System.Diagnostics;
using System.Reflection;
using AltLang;
using AltLang.Domain.Semantic;
using AltLang.Parser.Semantic;
using LabEntry.core;
using Lang.Domain;
using Lang.Lexer;
using Lang.Parser;
using Lang.RuleReader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Module = AltLang.Domain.Semantic.Module;

namespace LabEntry.Compiler;

public static class Compiler
{
    public static Dictionary<string, Module> CompileFromSource(string modulesPath)
    {
        var modules = new Dictionary<string, Module>();
        foreach (var headerFile in Directory.EnumerateFiles(modulesPath, "*.alth", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(headerFile);
            var text = File.ReadAllText(headerFile);
            var module = ModuleReader.ReadModule(name, text);
            modules.Add(headerFile, module);
        }
        return modules;
    }

    public static (List<ILangModule> reducers, SemanticAutomata finalAutomata) BuildReducers(Dictionary<string, Module> modules)
    {
        var automatas = new Dictionary<string, SemanticAutomata>();
        var nameToPath = modules.ToDictionary(p => p.Value.Name, p => p.Key);
        var reducers = new List<ILangModule>();
        foreach (var module in modules.Values.ToDictionary(m => m.Name).TopSort())
        {
            var automata = AutomataBuilder.MergeAutomata(module.Imports.Select(automatas.GetValueOrDefault)
                .Append(module.Automata).ToArray()!);
            automatas[module.Name] = automata;
            reducers.Add(CreateReducer(module.Name, nameToPath[module.Name], automata));
        }

        return (reducers, automatas[modules.Values.Last().Name]);
    }

    private static ILangModule CreateReducer(string moduleName, string modulePath, SemanticAutomata automata)
    {
        if (moduleName == "Core") return new CoreModule();
        if (moduleName == "Core.Definitions") return new CoreDefinitionsModule();
        if (moduleName == "Core.Numbers") return new CoreNumbersModule();
        if (moduleName == "Core.Macro") return new CoreMacroModule();
        if (File.Exists(modulePath.Replace(".alth", ".alt")))
        {
            var rText = File.ReadAllText(modulePath.Replace(".alth", ".alt"));
            var keywords = automata.KnownTokens
                .Where(t => t is Terminal {Type: TerminalType.Keyword})
                .Cast<Terminal>()
                .Select(t => t.Token);
            var tokens = Lexer.FromKeywords(keywords).Read(rText).ToList();
            var obj = automata.Read(tokens);
            return new AltModule(moduleName, obj!);
        }

        if (File.Exists(modulePath.Replace(".alth", ".alt.cs")))
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(modulePath.Replace(".alth", ".alt.cs")));
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

    private static IEnumerable<Module> TopSort(this Dictionary<string, Module> modules)
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
}