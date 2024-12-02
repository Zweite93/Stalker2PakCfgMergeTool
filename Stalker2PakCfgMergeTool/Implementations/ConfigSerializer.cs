using System.Text;
using Stalker2PakCfgMergeTool.Entities;
using Stalker2PakCfgMergeTool.Interfaces;

namespace Stalker2PakCfgMergeTool.Implementations;

public class ConfigSerializer : IConfigSerializer
{
    private const string StructBegin = "struct.begin";
    private const string StructEnd = "struct.end";

    public string Serialize(Config config)
    {
        var sb = new StringBuilder();
        SerializeLines(config.Values, sb, 0);

        var result = sb.ToString();

        return result;
    }

    public Config Deserialize(string fileName, string pakName, string configText)
    {
        // Remove BOM if present
        if (configText.Length > 0 && configText[0] == '\uFEFF')
        {
            configText = configText[1..];
        }

        var lines = configText.Split('\n').Select(line => line.Trim()).Where(line => !line.StartsWith("//") && !string.IsNullOrWhiteSpace(line)).ToList();
        var index = 0;

        var config = new Config
        {
            Name = fileName,
            PakName = pakName,

            Values = DeserializeLines(lines, ref index)
        };

        return config;
    }

    private static List<ConfigItem<object>> DeserializeLines(List<string> lines, ref int index)
    {
        var result = new List<ConfigItem<object>>();
        int? arrayIndex = null;

        while (index < lines.Count)
        {
            var line = lines[index];
            if (line.Contains(StructEnd))
            {
                index++;
                break;
            }

            // if it's an array item, need to use key + array index as and identifier
            // this is a workaround for stacks ("[*]") and arrays with duplicate indexes
            if (line.StartsWith('['))
            {
                arrayIndex = arrayIndex.HasValue ? arrayIndex + 1 : 0;
            }
            else
            {
                arrayIndex = null;
            }

            if (line.Contains(StructBegin))
            {
                var parts = line.Split(':', 2);

                var key = parts[0].Trim();
                var suffix = parts[1].Replace(StructBegin, string.Empty).Trim();


                index++;

                var nested = DeserializeLines(lines, ref index);

                var configStruct = new ConfigStructItem(key, key + arrayIndex, nested, suffix);

#if DEBUG
                foreach (var item in nested)
                {
                    item.Parent = configStruct;
                }
#endif
                result.Add(configStruct);
            }
            else
            {
                var parts = line.Split('=', 2);

                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    result.Add(new ConfigStringItem(key, key + arrayIndex, value));
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    // Unknown line format, just add it as is
                    result.Add(new ConfigStringItem(line, line, null));
                }

                index++;
            }
        }

        return result;
    }

    private static void SerializeLines(List<ConfigItem<object>> values, StringBuilder sb, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 3);

        foreach (var item in values)
        {
            switch (item)
            {
                case ConfigStructItem configStruct:
                    sb.AppendLine($"{indent}{configStruct.Key} : {StructBegin} {configStruct.Suffix}".TrimEnd());
                    SerializeLines(configStruct.Value, sb, indentLevel + 1);
                    sb.AppendLine(indent + StructEnd);
                    break;
                case ConfigStringItem configString:
                    var setValueStr = configString.Value == null ? string.Empty : $" = {configString.Value}";
                    sb.AppendLine($"{indent}{configString.Key}{setValueStr}".TrimEnd());
                    break;
                default:
                    var type = item.GetType();
                    throw new Exception($"Unexpected item type: {type}");
            }
        }
    }
}