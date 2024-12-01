namespace Stalker2PakCfgMergeTool.Entities;

public class Config
{
    public required string Name { get; set; }
    public required string PakName { get; set; }

    public required List<ConfigKeyValuePair<object?>> Values { get; set; }

    //public static List<ConfigKeyValuePair<object?>> GetValuesByOperationType(List<ConfigKeyValuePair<object?>> values, OperationType operationType)
    //{
    //    var result = new List<ConfigKeyValuePair<object?>>();
    //    foreach (var value in values)
    //    {
    //        switch (value.Value)
    //        {
    //            case ConfigStruct configStruct:
    //                var modifiedValues = GetValuesByOperationType(configStruct.Values, operationType);
    //                if (modifiedValues.Count > 0)
    //                {
    //                    result.Add(new ConfigKeyValuePair<object?>(value.Key, new ConfigStruct(value.Key, modifiedValues, configStruct.Suffix)));
    //                }
    //                break;
    //            default:
    //                if ((value.OperationType & operationType) != 0)
    //                {
    //                    result.Add(value);
    //                }
    //                break;
    //        }
    //    }


    //    return result.ToList();
    //}
}