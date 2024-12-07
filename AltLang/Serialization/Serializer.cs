using System.Collections;
using System.Text;
using AltLang.Domain.Semantic;
using LabEntry.domain;
using Lang.Domain;
using Lang.Domain.Semantic;
using Lang.Parser.Semantic;
using Lang.RuleReader.Semantic;

namespace Lang.RuleReader;

public static class Serializer
{
    public static void Serialize(BinaryWriter writer, Module module)
    {
        writer.Write(module.Name);
        writer.Write((byte)module.Imports.Count);
        foreach (var import in module.Imports) writer.Write(import);
        WriteSemanticAutomata(writer, module.Automata);
    }

    #region Write

    private static void WriteSemanticAutomata(BinaryWriter writer, SemanticAutomata automata)
    {
        writer.Write((byte)automata.KnownTokens.Count);
        foreach (var token in automata.KnownTokens) WriteToken(writer, token);
        writer.Write((byte) 127);
        WriteRules(writer, automata);
        writer.Write((byte) 127);
        writer.Write('\n');
        WriteAutomata(writer, automata);
    }

    private static void WriteRules(BinaryWriter writer, SemanticAutomata automata)
    {
        foreach (var rule in automata.Rules)
        {
            writer.Write(rule.Count);
            writer.Write(rule.Source.Name);
            WriteSemantic(writer, rule.Reduce);
        }
    }

    private static void WriteSemantic(BinaryWriter writer, SemanticObjectDefinition definition)
    {
        void WriteStructure(StructureDefinition def)
        {
            writer.Write("s"u8);
            switch (def)
            {
                case StructureNumberDefinition number:
                    writer.Write("n"u8);
                    writer.Write((byte) number.Number);
                    break;
                case ExplicitStructureDefinition exp:
                    writer.Write("e"u8);
                    WriteWord(exp.Name);
                    foreach (var property in exp.Properties) WriteProperty(property);
                    writer.Write('\n');
                    break;
            }
        }

        void WriteWord(WordDefinition def)
        {
            writer.Write("w"u8);
            switch (def)
            {
                case StructureNameDefinition number:
                    writer.Write("n"u8);
                    writer.Write((byte) number.Number);
                    break;
                case ExplicitWordDefinition exp:
                    writer.Write("e"u8);
                    writer.Write(exp.Word);
                    break;
            }
        }

        void WriteProperty(PropertyDefinition def)
        {
            switch (def)
            {
                case ExplicitPropertyDefinition exp:
                    writer.Write("e"u8);
                    WriteWord(exp.Key);
                    WriteSemantic(writer, exp.Object);
                    break;
                case NamelessPropertyDefinition nameless:
                    writer.Write("n"u8);
                    WriteSemantic(writer, nameless.Object);
                    break;
                case SpreadPropertyDefinition spread:
                    writer.Write("s"u8);
                    writer.Write((byte) spread.Number);
                    break;
            }
        }

        switch (definition)
        {
            case WordDefinition word:
                WriteWord(word);
                break;
            case StructureDefinition structure:
                WriteStructure(structure);
                break;
        }
    }

    private static void WriteAutomata(BinaryWriter writer, SemanticAutomata automata)
    {
        var stateToActions = automata.Actions.GroupBy(i => i.Key.Item1).OrderBy(g => g.Key);
        foreach (var group in stateToActions)
        {
            foreach (var ((_, token), action) in group)
            {
                WriteToken(writer, token);
                WriteAction(writer, action);
            }

            writer.Write('\n');
        }
    }

    private static void WriteAction(BinaryWriter writer, SemanticAutomata.Action action)
    {
        switch (action)
        {
            case SemanticAutomata.Reduce r:
                writer.Write(~r.Rule);
                break;
            case SemanticAutomata.Shift s:
                writer.Write(s.NextState);
                break;
            case SemanticAutomata.Accept:
                writer.Write(int.MinValue);
                break;
        }
    }

    private static void WriteToken(BinaryWriter writer, Token token)
    {
        writer.Write(token switch
        {
            NonTerminal => "n"u8,
            Terminal t => t.Type switch
            {
                TerminalType.Internal => "i"u8,
                TerminalType.Keyword => "k"u8,
                TerminalType.Word => "w"u8,
            }
        });
        writer.Write(token switch
        {
            NonTerminal n => n.Name,
            Terminal t => t.Token,
        });
    }

    #endregion

    #region Read

    public static Module Deserialize(BinaryReader reader)
    {
        var name = reader.ReadString();
        var count = reader.ReadByte();
        var imports = new List<string>();
        for (var i = 0; i < count; i++) imports.Add(reader.ReadString());
        var automata = ReadAutomata(reader);
        return new Module(name, imports, automata);
    }

    private static SemanticAutomata ReadAutomata(BinaryReader reader)
    {
        var knownTokens = new HashSet<Token>();
        var count = reader.ReadByte();
        for (var i = 0; i < count; i++) knownTokens.Add(ReadToken(reader));
        var automata = new SemanticAutomata
        {
            Rules = ReadRules(reader).ToList(),
            KnownTokens = knownTokens
        };
        reader.ReadByte();
        ReadAction(reader, automata.Actions);
        return automata;
    }

    private static void ReadAction(BinaryReader reader,
        Dictionary<(int, Token), SemanticAutomata.Action> automataActions)
    {
        var state = 0;
        while (reader.BaseStream.Position != reader.BaseStream.Length)
        {
            foreach (var (token, action) in ReadActionsForState(reader))
            {
                automataActions[(state, token)] = action;
            }

            state++;
        }
    }

    private static IEnumerable<(Token, SemanticAutomata.Action)> ReadActionsForState(BinaryReader reader)
    {
        while (true)
        {
            var descr = reader.ReadChar();
            if (descr == '\n') yield break;
            var name = reader.ReadString();
            var num = reader.ReadInt32();
            SemanticAutomata.Action act = num switch
            {
                int.MinValue => new SemanticAutomata.Accept(),
                < 0 => new SemanticAutomata.Reduce(~num, 0),
                _ => new SemanticAutomata.Shift(num, 0)
            };
            yield return descr switch
            {
                'n' => (new NonTerminal(name), act),
                'k' => (Terminal.Keyword(name), act),
                'w' => (Terminal.Word(name), act),
                'i' => (Terminal.End, act)
            };
        }
    }

    private static Token ReadToken(BinaryReader reader)
    {
        var descr = reader.ReadChar();
        var name = reader.ReadString();
        return descr switch
        {
            'n' => new NonTerminal(name),
            'k' => Terminal.Keyword(name),
            'w' => Terminal.Word(name),
            'i' => Terminal.End
        };
    }

    private static IEnumerable<SemanticRuleShort> ReadRules(BinaryReader reader)
    {
        while (true)
        {
            var count = reader.ReadByte();
            if (count == 127)
                yield break;
            var source = new NonTerminal(reader.ReadString());
            var def = ReadDefinition(reader);
            yield return new SemanticRuleShort(source, count, def);
        }
    }

    private static SemanticObjectDefinition ReadDefinition(BinaryReader reader)
    {
        WordDefinition ReadWord(BinaryReader reader, bool skipPrefix = false)
        {
            if (skipPrefix) reader.ReadByte();
            return reader.ReadChar() switch
            {
                'e' => new ExplicitWordDefinition(reader.ReadString()),
                'n' => new StructureNameDefinition(reader.ReadByte())
            };
        }

        StructureDefinition ReadStructure(BinaryReader reader, bool skipPrefix = false)
        {
            if (skipPrefix) reader.ReadByte();
            return reader.ReadChar() switch
            {
                'e' => new ExplicitStructureDefinition(ReadWord(reader, true), ReadProperties(reader).ToArray()),
                'n' => new StructureNumberDefinition(reader.ReadByte())
            };
        }

        IEnumerable<PropertyDefinition> ReadProperties(BinaryReader reader)
        {
            while (true)
            {
                switch (reader.ReadChar())
                {
                    case 'e':
                        yield return new ExplicitPropertyDefinition(ReadWord(reader, true), ReadDefinition(reader));
                        break;
                    case 'n':
                        yield return new NamelessPropertyDefinition(ReadDefinition(reader));
                        break;
                    case 's':
                        yield return new SpreadPropertyDefinition(reader.ReadByte());
                        break;
                    case '\n':
                        yield break;
                }
            }
        }

        return reader.ReadChar() switch
        {
            'w' => ReadWord(reader),
            's' => ReadStructure(reader)
        };
    }

    #endregion
}