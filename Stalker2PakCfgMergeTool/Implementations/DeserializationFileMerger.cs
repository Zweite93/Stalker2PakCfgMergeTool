using System.Text;
using Stalker2PakCfgMergeTool.Interfaces;

namespace Stalker2PakCfgMergeTool.Implementations;

public class DeserializationFileMerger : IFileMerger
{
    public string Merge(string originalText, List<string> modifiedTexts)
    {
        throw new NotImplementedException();
    }
}

public static class ConfigSerializer
{
    private const string StructBegin = "struct.begin";
    private const string StructEnd = "struct.end";

    public static List<KeyValuePair<string, ConfigObject>> Deserialize(string configText)
    {
        var lines = configText.Split('\n').Select(line => line.Trim()).Where(line => !line.StartsWith("//")).ToList();
        var index = 0;

        return DeserializeLines(lines, ref index);
    }

    private static List<KeyValuePair<string, ConfigObject>> DeserializeLines(List<string> lines, ref int index)
    {
        var result = new List<KeyValuePair<string, ConfigObject>>();

        while (index < lines.Count)
        {
            var line = lines[index];

            if (line.Contains(StructBegin))
            {
                var split = line.Split(':');
                var key = split[0].Trim();
                var annotation = split[1].Replace(StructBegin, string.Empty).TrimStart();

                index++;
                var nested = DeserializeLines(lines, ref index);
                result.Add(new KeyValuePair<string, ConfigObject>(key, new ConfigObject(nested, annotation)));
            }
            else if (line == StructEnd)
            {
                index++;
                break;
            }
            else
            {
                var parts = line.Split('=');
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    value = string.IsNullOrWhiteSpace(value) ? string.Empty : value;

                    result.Add(new KeyValuePair<string, ConfigObject>(key, new ConfigObject(value)));
                }

                index++;
            }
        }

        return result;
    }

    public static string Serialize(List<KeyValuePair<string, ConfigObject>> config)
    {
        var sb = new StringBuilder();
        SerializeLines(config, sb, 0);

        var result = sb.ToString();

        return result;
    }

    private static void SerializeLines(List<KeyValuePair<string, ConfigObject>> config, StringBuilder sb, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 3);

        foreach (var kvp in config)
        {
            if (kvp.Value.Value is List<KeyValuePair<string, ConfigObject>> nested)
            {
                sb.AppendLine($"{indent}{kvp.Key} : {StructBegin} {kvp.Value.Annotation}".TrimEnd());
                SerializeLines(nested, sb, indentLevel + 1);
                sb.AppendLine(indent + StructEnd);
            }
            else
            {
                sb.AppendLine($"{indent}{kvp.Key} = {kvp.Value.Value}".TrimEnd());
            }
        }
    }
}

public class ConfigObject
{
    public string Annotation { get; }
    public object Value { get; }

    public ConfigObject(object value, string annotation = "")
    {
        Value = value;
        Annotation = annotation;
    }
}