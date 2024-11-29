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
            if (line == StructEnd)
            {
                index++;
                break;
            }

            if (line.Contains(StructBegin))
            {
                var parts = line.Split(':', 2);

                var key = parts[0].Trim();
                var keySuffix = parts[1];

                index++;

                if (IsList(key) && (listIndex = int.Parse(key.TrimStart('[').TrimEnd(']'))) == 0)
                {
                    var list = new List<ConfigStruct?>();
                    while (index < lines.Count && lines[index - 1] is var previousLine && previousLine.TrimStart().StartsWith("["))
                    {
                        if (IsStructListEmptyItem(previousLine))
                        {
                            list.Add(null);
                            index++;
                            continue;
                        }

                        keySuffix = previousLine.Split(':', 2)[1];
                        var nested = DeserializeLines(lines, ref index);
                        list.Insert(listIndex, new ConfigStruct(string.Empty, nested, keySuffix));

                        index++;
                    }

                    result.Add(list);
                    index--;
                }
                else
                {
                    var nested = DeserializeLines(lines, ref index);
                    result.Add(new ConfigStruct(key, nested, keySuffix));
                }
            }
            else
            {
                var parts = line.Split('=', 2);

                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    if (IsList(key))
                    {
                        var listVales = new List<string> { value };

                        // Continue reading lines until the end of the list

                        while (++index < lines.Count)
                        {
                            var nextLine = lines[index].Trim();
                            if (nextLine.Contains('='))
                            {
                                var nextParts = nextLine.Split('=');
                                if (nextParts.Length == 2 && IsList(nextParts[0].Trim()))
                                {
                                    listVales.Add(nextParts[1].Trim());
                                }
                                else
                                {
                                    index--;
                                    break;
                                }
                            }
                            else
                            {
                                index--;
                                break;
                            }
                        }

                        result.Add(listVales);
                    }
                    else
                    {
                        result.Add(new KeyValuePair<string, string>(key, value));
                    }
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
                    sb.AppendLine($"{indent}{configStruct.Name} :{configStruct.NameSuffix}".TrimEnd());
                    SerializeLines(configStruct.Values, sb, indentLevel + 1);
                    sb.AppendLine(indent + StructEnd);
                    break;
                case KeyValuePair<string, string> kvp:
                    sb.AppendLine($"{indent}{kvp.Key} = {kvp.Value}".TrimEnd());
                    break;
                case List<string> listValues:
                    for (var i = 0; i < listValues.Count; i++)
                    {
                        sb.AppendLine($"{indent}[{i}] = {listValues[i]}".TrimEnd());
                    }
                    break;
                case List<ConfigStruct?> structList:
                    for (var i = 0; i < structList.Count; i++)
                    {
                        var listStruct = structList[i];
                        if (listStruct == null)
                        {
                            sb.AppendLine($"{indent}[{i}] =".TrimEnd());
                            continue;
                        }

                        sb.AppendLine($"{indent}[{i}] :{listStruct.NameSuffix}".TrimEnd());
                        SerializeLines(listStruct.Values, sb, indentLevel + 1);
                        sb.AppendLine(indent + StructEnd);
                    }
                    break;
            }
        }
    }

    private static bool IsList(string? str)
    {
        return Regex.IsMatch(str ?? "", @"\[[0-9]+\]");
    }

    // handle a special case where struct list item is empty, looks like "[0] ="
    // "why? because fuck you, that's why" GSC apparently
    private static bool IsStructListEmptyItem(string line)
    {
        var parts = line.Split('=').ToList();
        return parts.Count > 1 && parts[1] == string.Empty;
    }
}