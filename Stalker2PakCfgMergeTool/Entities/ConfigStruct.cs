namespace Stalker2PakCfgMergeTool.Entities;

public class ConfigStruct
{
    public string Name { get; }
    public object Values { get; }
    public string Annotation { get; }

    public ConfigStruct(string name, object values, string annotation = "")
    {
        Name = name;
        Values = values;
        Annotation = annotation;
    }
}