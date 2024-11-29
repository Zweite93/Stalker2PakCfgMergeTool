namespace Stalker2PakCfgMergeTool.Entities;

public class ConfigStruct
{
    public string Key { get; }
    public List<object> Values { get; }
    public string KeySuffix { get; }

    public ConfigStruct(string key, List<object> values, string keySuffix = "")
    {
        Key = key;
        Values = values;
        KeySuffix = keySuffix;
    }
}