using System.Text;
using Stalker2PakCfgMergeTool.Entities;
using Stalker2PakCfgMergeTool.Extensions;
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
        var lines = configText
            .RemoveBom()
            .Split('\n')
            .Select(line => line.RemoveComments().Trim())
            .Where(line => !line.StartsWith("//") && !string.IsNullOrWhiteSpace(line))
            .ToList();

        var index = 0;

        var config = new Config
        {
            Name = fileName,
            PakName = pakName,

            Values = DeserializeLines(lines, ref index)
        };

        return config;
    }

    private static List<ConfigItem> DeserializeLines(List<string> lines, ref int index)
    {
        var result = new List<ConfigItem>();
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
                var refInfo = ParseRefInfo(parts[1]);

                index++;

                var nested = DeserializeLines(lines, ref index);
                var configStruct = new ConfigStructItem(key, key + arrayIndex, nested, refInfo);

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

    private static void SerializeLines(List<ConfigItem> values, StringBuilder sb, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 3);

        foreach (var item in values)
        {
            switch (item)
            {
                case ConfigStructItem configStruct:
                    sb.AppendLine($"{indent}{configStruct.Key} : {StructBegin}{RefInfoToString(configStruct.RefInfo)}".TrimEnd());
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

    private static RefInfo? ParseRefInfo(string refInfoStr)
    {
        if (string.IsNullOrWhiteSpace(refInfoStr))
        {
            return null;
        }

        // example {refulr=../fileName.cfg; refkey=structName; bskipref}

        var parts = refInfoStr
            .Replace(StructBegin, string.Empty)
            .Replace("{", string.Empty)
            .Replace("}", string.Empty)
            .Trim()
            .Split(';')
            .Select(x => x.Trim())
            .ToDictionary(x => x.Split('=')[0], x => x.Split('=').ElementAtOrDefault(1));

        return new RefInfo
        {
            RefUrl = parts.GetValueOrDefault("refurl"),
            RefKey = parts.GetValueOrDefault("refkey"),
            SkipRef = parts.ContainsKey("bskipref")
        };
    }

    private static string? RefInfoToString(RefInfo? refInfo)
    {
        if (refInfo == null)
        {
            return null;
        }

        var refInfoStr = $"{(refInfo.RefUrl == null ? string.Empty : $"refurl={refInfo.RefUrl};")}{(refInfo.RefKey == null ? string.Empty : $"refkey={refInfo.RefKey};")}{(refInfo.SkipRef ? "bskipref" : string.Empty)}".TrimEnd(';');

        return refInfoStr == string.Empty ? string.Empty : $" {{{refInfoStr}}}";
    }
}