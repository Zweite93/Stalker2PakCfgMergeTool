namespace Stalker2PakCfgMergeTool.Entities;

public class ConfigKeyValuePair<TKey, TValue>
{
#if DEBUG
    public ConfigStruct? Parent { get; set; }
#endif

    public TKey Key { get; }
    public TValue Value { get; set; }
    public OperationType OperationType { get; set; } = OperationType.Unchanged;

    public ConfigKeyValuePair(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }
}