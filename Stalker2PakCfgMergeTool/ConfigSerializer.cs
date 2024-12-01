using System.Text;
using Stalker2PakCfgMergeTool.Entities;

namespace Stalker2PakCfgMergeTool;

public static class ConfigSerializer
{
    private const string StructBegin = "struct.begin";
    private const string StructEnd = "struct.end";

    public static Config Deserialize(string fileName, string pakName, string configText)
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
            // TODO: Add name and pakName to Deserialize signature, set them here
            Name = fileName,
            PakName = pakName,

            Values = DeserializeLines(lines, ref index, fileName, pakName)
        };

        return config;
    }

    private static List<ConfigItem<object>> DeserializeLines(List<string> lines, ref int index, string fileName, string pakName)
    {
        var result = new List<ConfigItem<object>>();

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
                var parts = line.Split(':', 2);

                var key = parts[0].Trim();
                var suffix = parts[1].Replace(StructBegin, string.Empty).Trim();

                index++;

                var nested = DeserializeLines(lines, ref index, fileName, pakName);

                string id;

                // if struct is in array, find ID and SID for identification
                if (key.StartsWith('['))
                {
                    id = nested.FirstOrDefault(kvp => string.Equals(kvp.Key, "ID", StringComparison.InvariantCultureIgnoreCase))?.Value?.ToString() ??
                        // If ID is not available, try finding SID (string ID I presume)
                        nested.FirstOrDefault(kvp => string.Equals(kvp.Key, "SID", StringComparison.InvariantCultureIgnoreCase))?.Value?.ToString() ??
                        // if SID is not available, try finding words that contains are ending with ID or IDs, this will include SID as well. Need to be case-sensitive to avoid matching with other words.
                        nested.FirstOrDefault(kvp => kvp.Key.EndsWith("ID", StringComparison.InvariantCulture) || kvp.Key.EndsWith("IDs", StringComparison.InvariantCulture))?.Value?.ToString() ??
                        // If everything fails, use key as ID. Could cause problems if this struct is used when merging.
                        key;

                    // these are special because there is no way to identify them, not even by index. I need to have SOMETHING for identification, so I'm using the first nested value.
                    if (key == "[*]")
                    {
                        var firstNested = nested.FirstOrDefault();
                        id = (firstNested as ConfigStringItem)?.Value ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(id))
                        {
                            Console.WriteLine($"{fileName} | {pakName} | Line {index + 1}: No nested values found for [*] struct. This file is not supported, merge at your own risk.");
                            id = key;
                        }
                    }
                }
                else
                {
                    id = key;
                }

                var configStruct = new ConfigStructItem(key, id, nested, suffix);

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

                    string id;

                    // yes this shit again
                    if (key == "[*]")
                    {
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            Console.WriteLine($"{fileName} | {pakName} | Line {index + 1}: No nested values found for [*] struct. This file is not supported, merge at your own risk.");
                        }

                        id = key + value;
                    }
                    else
                    {
                        id = key;
                    }

                    result.Add(new ConfigStringItem(key, id, value));
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

    public static string Serialize(Config config)
    {
        var sb = new StringBuilder();
        SerializeLines(config.Values, sb, 0);

        var result = sb.ToString();

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
                    throw new Exception("Unexpected item type");
            }
        }
    }
}