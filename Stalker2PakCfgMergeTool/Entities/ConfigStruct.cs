namespace Stalker2PakCfgMergeTool.Entities;

public class ConfigStruct
{
#if DEBUG
    public ConfigStruct? Parent { get; set; }
#endif

    public string Key { get; set; }
    public string Suffix { get; }
    public List<ConfigKeyValuePair<string, object?>> Values { get; }

    public ConfigStruct(string key, List<ConfigKeyValuePair<string, object?>> values, string suffix = "")
    {
        Key = key;
        Values = values;
        Suffix = suffix;
    }

    public string? FindId()
    {
        return (string?)Values.FirstOrDefault(kvp => kvp.Key == "ID")?.Value;
    }

    public string? FindSid()
    {
        return (string?)Values.FirstOrDefault(kvp => kvp.Key == "SID")?.Value;
    }
}