
namespace AltLang.Serialization.Semantic;

public abstract record ObjectDefinition;

public abstract record WordDefinition : ObjectDefinition;
public record StructureNameDefinition(int Number) : WordDefinition;
public record ExplicitWordDefinition(string Word) : WordDefinition;

public abstract record StructureDefinition : ObjectDefinition;
public record StructureNumberDefinition(int Number) : StructureDefinition;
public record ExplicitStructureDefinition(WordDefinition Name, PropertyDefinition[] Properties) : StructureDefinition;

public abstract record PropertyDefinition;
public record ExplicitPropertyDefinition(WordDefinition Key, ObjectDefinition Object) : PropertyDefinition;
public record NamelessPropertyDefinition(ObjectDefinition Object) : PropertyDefinition;
public record SpreadPropertyDefinition(int Number) : PropertyDefinition;