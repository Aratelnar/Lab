using LabEntry.domain;

namespace AltLang;

public interface ILangModule
{
    public string ModuleName { get; }
    public SemanticObject? Reduce(SemanticObject obj, Func<SemanticObject, SemanticObject> reduce);
}