using Lang.Parser.Semantic;

namespace AltLang.Domain.Semantic;

public record Module(string Name, List<string> Imports, SemanticAutomata Automata);