using System.Diagnostics.CodeAnalysis;

namespace AltLang.Serialization.Semantic;

public static class ObjectDefinitionReader
{
    public static ObjectDefinition Read(string text)
    {
        var position = 0;
        return ReadStruct(text, ref position);
    }

    private static WordDefinition ReadWord(string text, ref int position)
    {
        var word = ReadWhile(c => char.IsLetterOrDigit(c) || c == '_', text, ref position);

        if (!string.IsNullOrEmpty(word))
            return new ExplicitWordDefinition(word);
        ReadChar('!', text, ref position);
        var number = ReadInt(text, ref position);
        return new StructureNameDefinition(number);
    }

    private static ObjectDefinition ReadStruct(string text, ref int position)
    {
        if (char.IsDigit(text[position]))
            return new StructureNumberDefinition(ReadInt(text, ref position));

        WordDefinition word = new ExplicitWordDefinition("");
        if (text[position] != '{')
            word = ReadWord(text, ref position);

        if (position == text.Length || text[position] != '{')
            return word;
        ReadChar('{', text, ref position);
        var properties = new List<PropertyDefinition>();
        while (TryReadKey(text, ref position, out var key))
            properties.Add(key);

        ReadChar('}', text, ref position);
        return new ExplicitStructureDefinition(word, properties.ToArray());
    }

    private static bool TryReadKey(string text, ref int position, [MaybeNullWhen(false)] out PropertyDefinition key)
    {
        if (text[position] == ',') position++;
        key = null;
        var c = text[position];
        if (!char.IsLetter(c) && c != '_' && c != '.' && c != '*' && c != '!') return false;
        if (c == '.')
        {
            ReadChar('.', text, ref position);
            ReadChar('.', text, ref position);
            ReadChar('.', text, ref position);
            var number = ReadInt(text, ref position);
            key = new SpreadPropertyDefinition(number);
            return true;
        }

        var word = c != '*' ? ReadWord(text, ref position) : null;
        if (c == '*') position++;
        ReadChar(':', text, ref position);
        var obj = ReadStruct(text, ref position);
        key = word == null ? new NamelessPropertyDefinition(obj) : new ExplicitPropertyDefinition(word, obj);
        return true;
    }

    private static void ReadChar(char c, string text, ref int position)
    {
        if (text[position] != c) throw new ArgumentException();
        position++;
    }

    private static int ReadInt(string text, ref int position)
    {
        if (!char.IsDigit(text[position])) throw new ArgumentException();
        return int.Parse(ReadWhile(char.IsDigit, text, ref position));
    }

    private static string ReadWhile(Predicate<char> condition, string text, ref int position)
    {
        var start = position;
        var c = text[position];
        while (condition(c))
        {
            position++;
            if (position == text.Length) break;
            c = text[position];
        }

        return text[start..position];
    }
}