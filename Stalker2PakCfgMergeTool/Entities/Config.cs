using Stalker2PakCfgMergeTool.Enums;

namespace Stalker2PakCfgMergeTool.Entities;

public abstract class ConfigItem<T> where T : class
{
    /// <summary>
    /// Unique identifier of the config struct. Can be an ID, SID, word that contain ID[s] or SID[s], a key, or a first nested key + value. Check Serialize method for more info.
    /// Reason for this is that some configs structs use array index as their key, and we need to identify them somehow.
    /// EXAMPLE: 
    /// </summary>
    public string Id { get; }

#if DEBUG
    public ConfigStructItem? Parent { get; set; }
#endif

    public OperationType OperationType { get; set; } = OperationType.Unchanged;

    public string Key { get; }
    public abstract T? Value { get; }

    protected ConfigItem(string key, string id)
    {
        Key = key;
        Id = id;
    }
}

public class ConfigStringItem : ConfigItem<object>
{
    public override string? Value {get;}

    public ConfigStringItem(string key, string id, string? value) : base(key, id) 
    {
        Value = value;
    }
}

public class ConfigStructItem : ConfigItem<object>
{

    /// <summary>
    /// Some structs have additional parameters that are declared after struct.begin. 
    /// EXAMPLE: "Bolt : struct.begin {refkey=Empty}"
    /// </summary>
    public string Suffix { get; }

    public override List<ConfigItem<object>> Value { get; }

    public ConfigStructItem(string key, string id, List<ConfigItem<object>> values, string suffix = "") : base(key, id)
    {
        Value = values;
        Suffix = suffix;
    }
}

public class Config
{
    public required string Name { get; set; }
    public required string PakName { get; set; }

    public required List<ConfigItem<object>> Values { get; set; }

    public static List<ConfigItem<object>> GetValuesByOperationType(List<ConfigItem<object>> values, OperationType operationType)
    {
        var result = new List<ConfigItem<object>>();
        foreach (var value in values)
        {
            switch (value.Value)
            {
                case ConfigStructItem configStruct:
                    var modifiedValues = GetValuesByOperationType(configStruct.Value, operationType);
                    if (modifiedValues.Count > 0)
                    {
                        result.Add(new ConfigStructItem(configStruct.Key, configStruct.Id, modifiedValues, configStruct.Suffix));
                    }
                    break;
                default:
                    if ((value.OperationType & operationType) != 0)
                    {
                        result.Add(value);
                    }
                    break;
            }
        }


        return result.ToList();
    }
}