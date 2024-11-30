using Stalker2PakCfgMergeTool.Entities;
using Stalker2PakCfgMergeTool.Interfaces;

namespace Stalker2PakCfgMergeTool.Implementations;

public class DeserializationFileMerger : IFileMerger
{
    public (string originalText, string mergedText) Merge(string originalText, List<string> modifiedTexts)
    {
        var originalConfig = ConfigSerializer.Deserialize(originalText);
        var modifiedConfigs = modifiedTexts.Select(ConfigSerializer.Deserialize).ToList();
        var mergedConfig = new Config { Values = [] };

        foreach (var modifiedConfig in modifiedConfigs)
        {
            if (modifiedConfig.Values.Count == 0)
            {
                continue;
            }

            MergeConfigValues(originalConfig.Values, modifiedConfig.Values, mergedConfig.Values);
        }

        originalText = ConfigSerializer.Serialize(originalConfig);
        var mergedText = ConfigSerializer.Serialize(mergedConfig);

        return (originalText, mergedText);
    }

    private static void MergeConfigValues(List<ConfigKeyValuePair<string, object?>> originalConfigValues, List<ConfigKeyValuePair<string, object?>> modifiedConfigValues, List<ConfigKeyValuePair<string, object?>> mergedConfigValues)
    {

        var modifiedKeys = new HashSet<string>(modifiedConfigValues.Select(v => v.Key));
        foreach (var originalValue in originalConfigValues.Where(originalValue => originalValue.OperationType != OperationType.Deleted && !modifiedKeys.Contains(originalValue.Key)))
        {
            originalValue.OperationType = OperationType.Deleted;
        }

        foreach (var modifiedValue in modifiedConfigValues)
        {
            if (string.Equals(modifiedValue.Value, "ChemicalDamage"))
            {

            }

            switch (modifiedValue.Value)
            {
                case ConfigStruct modifiedStruct:
                    MergeConfigStruct(originalConfigValues, mergedConfigValues, modifiedValue, modifiedStruct);
                    continue;
                case string:
                case null:
                    MergeConfigKeyValuePair(originalConfigValues, mergedConfigValues, modifiedValue);
                    continue;
                default:
                    throw new Exception($"Unknown type: {modifiedValue.Value.GetType()}");
            }
        }

        //return operationType;
    }

    private static void MergeConfigStruct(List<ConfigKeyValuePair<string, object?>> originalConfigValues, List<ConfigKeyValuePair<string, object?>> mergedConfigValues, ConfigKeyValuePair<string, object?> modifiedValue, ConfigStruct modifiedStruct)
    {
        var originalStruct = FindMatchingStruct(modifiedValue.Key, modifiedStruct, originalConfigValues);
        var mergedStruct = FindMatchingStruct(modifiedValue.Key, modifiedStruct, mergedConfigValues);

        //if (modifiedValue is { Key: "InteractionEffectPrototypeSIDs", Parent.Key: "ChemicalAnomaly" })
        //{

        //}

        if (originalStruct != null && mergedStruct != null)
        {
            MergeConfigValues(originalStruct.Values, modifiedStruct.Values, mergedStruct.Values);
            return;
        }

        if (originalStruct != null)
        {
            var newMergedStruct = new ConfigStruct(modifiedValue.Key, [], modifiedStruct.Suffix);
            MergeConfigValues(originalStruct.Values, modifiedStruct.Values, newMergedStruct.Values);
            var newMergedValue = new ConfigKeyValuePair<string, object?>(modifiedValue.Key, newMergedStruct);

            mergedConfigValues.Add(newMergedValue);
            return;
        }

        modifiedValue.OperationType = OperationType.Added;
        mergedConfigValues.Add(modifiedValue);
    }

    private static void MergeConfigKeyValuePair(List<ConfigKeyValuePair<string, object?>> originalConfigValues, List<ConfigKeyValuePair<string, object?>> mergedConfigValues, ConfigKeyValuePair<string, object?> modifiedValue)
    {
        var originalValue = originalConfigValues.FirstOrDefault(v => v.Key == modifiedValue.Key);
        if (originalValue != null && originalValue.OperationType != OperationType.Unchanged)
        {
            return;
        }

        var mergedValue = mergedConfigValues.FirstOrDefault(v => v.Key == modifiedValue.Key);
        if (mergedValue != null && mergedValue.OperationType != OperationType.Unchanged)
        {
            return;
        }

        var operationType = Equals(originalValue?.Value, modifiedValue.Value) ? OperationType.Unchanged : OperationType.Modified;

        if (originalValue != null && mergedValue != null)
        {
            mergedValue.Value = modifiedValue.Value;
            mergedValue.OperationType = operationType;
            originalValue.OperationType = operationType;
            return;
        }

        if (originalValue != null)
        {
            modifiedValue.OperationType = operationType;
            modifiedValue.Value = modifiedValue.Value;
            mergedConfigValues.Add(modifiedValue);
            return;
        }

        modifiedValue.OperationType = OperationType.Added;
        mergedConfigValues.Add(modifiedValue);
        return;
    }

    private static ConfigStruct? FindMatchingStruct(string key, ConfigStruct configStruct, List<ConfigKeyValuePair<string, object?>> otherValues)
    {
        if (key.StartsWith('['))
        {
            var id = configStruct.FindId();
            if (!string.IsNullOrEmpty(id))
            {
                var structById = otherValues.FirstOrDefault(v => v.Value is ConfigStruct s && Equals(s.FindId(), id));
                if (structById != null)
                {
                    return (ConfigStruct)structById.Value!;
                }
            }

            var sid = configStruct.FindSid();
            if (!string.IsNullOrEmpty(sid))
            {
                var structBySid = otherValues.FirstOrDefault(v => v.Value is ConfigStruct s && Equals(s.FindSid(), sid));
                if (structBySid != null)
                {
                    return (ConfigStruct)structBySid.Value!;
                }
            }

        }

        return otherValues.FirstOrDefault(v => v.Key == key && v.Value is ConfigStruct)?.Value as ConfigStruct;
    }
}