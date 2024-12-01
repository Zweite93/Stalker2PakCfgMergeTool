namespace Stalker2PakCfgMergeTool.Entities;

public class ConfigKeyValuePair<TValue>
{
#if DEBUG
    public ConfigStruct? Parent { get; set; }
#endif

    public OperationType OperationType { get; set; } = OperationType.Unchanged;

    public string Key { get; }
    public TValue Value { get; }

    public ConfigKeyValuePair(string key, TValue value)
    {
        Key = key;
        Value = value;
    }
}