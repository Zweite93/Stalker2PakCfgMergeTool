namespace Stalker2PakCfgMergeTool.Entities;

public class Config
{
    public required List<ConfigKeyValuePair<string, object?>> Values { get; set; }

    public static List<ConfigKeyValuePair<string, object?>> GetValuesByOperationType(List<ConfigKeyValuePair<string, object?>> values, OperationType operationType)
    {
        var result = new List<ConfigKeyValuePair<string, object?>>();
        foreach (var value in values)
        {
            switch (value.Value)
            {
                case ConfigStruct configStruct:
                    var modifiedValues = GetValuesByOperationType(configStruct.Values, operationType);
                    if (modifiedValues.Count > 0)
                    {
                        result.Add(new ConfigKeyValuePair<string, object?>(value.Key, new ConfigStruct(modifiedValues, configStruct.Suffix)));
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