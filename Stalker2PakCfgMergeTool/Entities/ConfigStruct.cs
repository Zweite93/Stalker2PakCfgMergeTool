namespace Stalker2PakCfgMergeTool.Entities;

public class ConfigStruct
{
    public string Name { get; }
    public List<object> Values { get; }
    public string NameSuffix { get; }

    public ConfigStruct(string name, List<object> values, string nameSuffix = "")
    {
        Name = name;
        Values = values;
        NameSuffix = nameSuffix;
    }
}