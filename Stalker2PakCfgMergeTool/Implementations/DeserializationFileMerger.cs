using Stalker2PakCfgMergeTool.Interfaces;

namespace Stalker2PakCfgMergeTool.Implementations;

public class DeserializationFileMerger : IFileMerger
{
    public string Merge(string originalText, List<string> modifiedTexts)
    {
        //var originalConfig = ConfigSerializer.Deserialize(originalText);
        //var mergedConfig = new List<KeyValuePair<string, ConfigObject>>(originalConfig);

        //var modifiedConfigs = modifiedTexts.Select(ConfigSerializer.Deserialize).ToList();

        //foreach (var modifiedConfig in modifiedConfigs)
        //{
        //    if (modifiedConfig.Count == 0)
        //    {
        //        continue;
        //    }


        //}

        throw new NotImplementedException();
    }
}