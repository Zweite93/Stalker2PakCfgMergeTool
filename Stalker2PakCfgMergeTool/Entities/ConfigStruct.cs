namespace Stalker2PakCfgMergeTool.Entities;

public class ConfigStruct : ConfigValueBase<List<ConfigKeyValuePair<object?>>>
{
#if DEBUG
    public ConfigStruct? Parent { get; set; }
#endif

    /// <summary>
    /// Unique identifier of the config struct. It is a combination of Key, ID and SID.
    /// Reason for this is that some configs structs use array index as their key, and we need to identify them somehow.
    /// EXAMPLE: 
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Some structs have additional parameters that are declared after struct.begin. 
    /// EXAMPLE: "Bolt : struct.begin {refkey=Empty}"
    /// </summary>
    public string Suffix { get; }

    public ConfigStruct(string key, string id, string sid, List<ConfigKeyValuePair<object?>> value, string suffix = "") : base(key, value)
    {
        Id = Key + id + sid;
        Suffix = suffix;
    }

    public ConfigStruct(string key, string id, string suffix = "") : base(key, [])
    {
        Id = id;
        Suffix = suffix;
    }
}