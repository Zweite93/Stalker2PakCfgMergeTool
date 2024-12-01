namespace Stalker2PakCfgMergeTool.Entities;

public class ConfigValueBase<T>
{
    public string Key { get; }
    public OperationType OperationType { get; set; } = OperationType.Unchanged;
    public T Value { get; }

    public ConfigValueBase(string key, T value)
    {
        Key = key;
        Value = value;
    }
}