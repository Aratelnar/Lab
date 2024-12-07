
namespace Lang.RuleReader.Semantic;

public abstract record SemanticObjectDefinition;

public abstract record WordDefinition : SemanticObjectDefinition;
public record StructureNameDefinition(int Number) : WordDefinition;
public record ExplicitWordDefinition(string Word) : WordDefinition;

public abstract record StructureDefinition : SemanticObjectDefinition;
public record StructureNumberDefinition(int Number) : StructureDefinition;
public record ExplicitStructureDefinition(WordDefinition Name, PropertyDefinition[] Properties) : StructureDefinition;

public abstract record PropertyDefinition;
public record ExplicitPropertyDefinition(WordDefinition Key, SemanticObjectDefinition Object) : PropertyDefinition;
public record NamelessPropertyDefinition(SemanticObjectDefinition Object) : PropertyDefinition;
public record SpreadPropertyDefinition(int Number) : PropertyDefinition;