using System.Text;
using System.Text.RegularExpressions;
using Stalker2PakCfgMergeTool.Entities;

namespace Stalker2PakCfgMergeTool;

public static class ConfigSerializer
{
    private const string StructBegin = "struct.begin";
    private const string StructEnd = "struct.end";

    public static List<object> Deserialize(string configText)
    {
        // Remove BOM if present
        if (configText.Length > 0 && configText[0] == '\uFEFF')
        {
            configText = configText[1..];
        }

        var lines = configText.Split('\n').Select(line => line.Trim()).Where(line => !line.StartsWith("//")).ToList();
        var index = 0;

        var config = DeserializeLines(lines, ref index);

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
                var parts = line.Split(':');

                var key = parts[0].Trim();
                var annotation = parts[1].Replace(StructBegin, string.Empty).TrimStart();

                index++;

                if (IsList(key) && int.Parse(key.TrimStart('[').TrimEnd(']')) == 0)
                {
                    var list = new List<object?>();
                    while (index < lines.Count && lines[index - 1] is var previousLine && previousLine.TrimStart().StartsWith("["))
                    {
                        if (IsStructListEmptyItem(previousLine))
                        {
                            list.Add(null);
                            index++;
                            continue;
                        }

                        annotation = previousLine.Split(':')[1].Replace(StructBegin, string.Empty).TrimStart();
                        var nested = DeserializeLines(lines, ref index);
                        list.Add(new ConfigStruct(string.Empty, nested, annotation));

                        index++;
                    }

                    result.Add(list);
                    index--;
                }
                else
                {
                    var nested = DeserializeLines(lines, ref index);
                    result.Add(new ConfigStruct(key, nested, annotation));
                }
            }
            else
            {
                var parts = line.Split('=');

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

    public static string Serialize(List<object> config)
    {
        var sb = new StringBuilder();
        SerializeLines(config, sb, 0);

        var result = sb.ToString();

        return result;
    }

    private static void SerializeLines(List<object> config, StringBuilder sb, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 3);

        foreach (var item in config)
        {
            switch (item)
            {
                case ConfigStruct { Values: List<object> nested } configStruct:
                    sb.AppendLine($"{indent}{configStruct.Name} : {StructBegin} {configStruct.Annotation}".TrimEnd());
                    SerializeLines(nested, sb, indentLevel + 1);
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
                case List<object?> listStructs:
                    for (var i = 0; i < listStructs.Count; i++)
                    {
                        var listStruct = listStructs[i];
                        if (listStruct == null)
                        {
                            sb.AppendLine($"{indent}[{i}] =".TrimEnd());
                            continue;
                        }

                        var configStruct = (ConfigStruct)listStruct;
                        sb.AppendLine($"{indent}[{i}] : {StructBegin} {configStruct.Annotation}".TrimEnd());
                        SerializeLines((configStruct.Values as List<object>)!, sb, indentLevel + 1);
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