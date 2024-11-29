using System.Text;
using System.Text.RegularExpressions;
using Stalker2PakCfgMergeTool.Entities;

namespace Stalker2PakCfgMergeTool;

public static class ConfigSerializer
{
    private const string StructBegin = "struct.begin";
    private const string StructEnd = "struct.end";

    public static Config Deserialize(string configText)
    {
        // Remove BOM if present
        if (configText.Length > 0 && configText[0] == '\uFEFF')
        {
            configText = configText[1..];
        }

        var lines = configText.Split('\n').Select(line => line.Trim()).Where(line => !line.StartsWith("//")).ToList();
        var index = 0;

        var config = new Config
        {
            Values = DeserializeLines(lines, ref index)
        };

        return config;
    }

    private static List<object> DeserializeLines(List<string> lines, ref int index)
    {
        var result = new List<object>();

        while (index < lines.Count)
        {
            var line = lines[index];
            if (line.Contains(StructEnd))
            {
                index++;
                break;
            }

            if (line.Contains(StructBegin))
            {
                var parts = Regex.Split(line, @"(?=\s*:)");

                var key = parts[0].Trim();
                var keySuffix = string.Join("", parts.Skip(1));

                index++;

                var nested = DeserializeLines(lines, ref index);
                result.Add(new ConfigStruct(key, nested, keySuffix));
            }
            else
            {
                var parts = line.Split('=', 2);

                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    result.Add(new KeyValuePair<string, string>(key, value));
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    // Unknown line format, just add it as is
                    result.Add(line);
                }

                index++;
            }
        }

        return result;
    }

    public static string Serialize(Config config)
    {
        var sb = new StringBuilder();
        SerializeLines(config.Values, sb, 0);

        var result = sb.ToString();

        return result;
    }

    private static void SerializeLines(List<object> values, StringBuilder sb, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 3);

        foreach (var item in values)
        {
            switch (item)
            {
                case ConfigStruct configStruct:
                    sb.AppendLine($"{indent}{configStruct.Key}{configStruct.KeySuffix}".TrimEnd());
                    SerializeLines(configStruct.Values, sb, indentLevel + 1);
                    sb.AppendLine(indent + StructEnd);
                    break;
                case KeyValuePair<string, string> kvp:
                    sb.AppendLine($"{indent}{kvp.Key} = {kvp.Value}".TrimEnd());
                    break;
                case string line:
                    sb.AppendLine($"{indent}{line}".TrimEnd());
                    break;

                default:
                    throw new Exception("Unexpected item type");
            }
        }
    }
}