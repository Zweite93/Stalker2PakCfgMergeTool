using Stalker2PakCfgMergeTool.Entities;
using Stalker2PakCfgMergeTool.Enums;
using Stalker2PakCfgMergeTool.Interfaces;

namespace Stalker2PakCfgMergeTool.Implementations;

// not the most efficient implementation, but it works and is somewhat readable
// changes history can be later used to implement an actual UI where users will be able to choose which changes to apply
public class DeserializationFileMerger : IFileMerger
{
    private readonly IConfigSerializer _configSerializer;
    private readonly IConfigSerializerVerifier _configSerializerVerifier;

    public DeserializationFileMerger(IConfigSerializer configSerializer, IConfigSerializerVerifier configSerializerVerifier)
    {
        _configSerializer = configSerializer;
        _configSerializerVerifier = configSerializerVerifier;
    }

    public (string originalText, string mergedText) Merge(string originalText, string fileName, List<(string pakName, string modifiedText)> modifiedTexts)
    {
        var originalConfig = _configSerializer.Deserialize(fileName, "original", originalText);

        VerifyConfig(originalConfig, originalText);

        var modifiedConfigs = modifiedTexts.Select(mt =>
        {
            var modifiedConfig = _configSerializer.Deserialize(fileName, mt.pakName, mt.modifiedText);
            VerifyConfig(modifiedConfig, mt.modifiedText);
            return modifiedConfig;
        }).ToList();

        var changesHistoryList = new List<Config>();

        foreach (var modifiedConfig in modifiedConfigs)
        {
            var changesHistory = new Config { Name = modifiedConfig.Name, PakName = Constants.MergedPakBaseName, Values = [] };
            BuildChangesHistory(originalConfig.Values, modifiedConfig.Values, changesHistory.Values);

            changesHistoryList.Add(changesHistory);
        }

        originalText = _configSerializer.Serialize(originalConfig);

        foreach (var changesHistory in changesHistoryList)
        {
            ApplyChangesHistory(originalConfig.Values, changesHistory.Values);
        }

        var mergedText = _configSerializer.Serialize(originalConfig);

        return (originalText, mergedText);
    }

    private void VerifyConfig(Config config, string configText)
    {
        var verificationResult = _configSerializerVerifier.Verify(config, configText);
        if (!verificationResult.Success)
        {
            Console.WriteLine($"{config.PakName} | {config.Name} verification failed. This file might not merge correctly.");
        }
    }

    private static OperationType BuildChangesHistory(List<ConfigItem<object>> originalConfigValues, List<ConfigItem<object>> modifiedConfigValues, List<ConfigItem<object>> changesHistory)
    {
        var operationType = OperationType.Unchanged;
        var keysInModifiedConfig = new HashSet<string>();

        foreach (var modifiedValue in modifiedConfigValues)
        {
            OperationType mergeOperationTypeResult;
            switch (modifiedValue)
            {
                case ConfigStructItem modifiedStruct:
                    mergeOperationTypeResult = MergeConfigStruct(originalConfigValues, changesHistory, modifiedStruct);
                    keysInModifiedConfig.Add(modifiedStruct.Id);
                    break;
                case ConfigStringItem configString:
                    mergeOperationTypeResult = MergeConfigString(originalConfigValues, changesHistory, configString);
                    keysInModifiedConfig.Add(configString.Id);
                    break;
                default:
                    throw new Exception($"Unknown type: {modifiedValue.Value?.GetType()}");
            }

            if (mergeOperationTypeResult != OperationType.Unchanged)
            {
                operationType = OperationType.Modified;
            }
        }

        // find deleted values and add them to changes history
        var deletedStructs = originalConfigValues.Where(originalValue => originalValue is ConfigStructItem originalStruct && !keysInModifiedConfig.Contains(originalStruct.Id) && originalStruct.OperationType != OperationType.Deleted).ToList();
        var deletedStrings = originalConfigValues.Where(originalValue => originalValue is ConfigStringItem originalString && !keysInModifiedConfig.Contains(originalString.Id) && originalString.OperationType != OperationType.Deleted).ToList();

        if (deletedStructs.Count > 0 || deletedStrings.Count > 0)
        {
            operationType = OperationType.Modified;
        }

        var deletedStructsCopy = deletedStructs.Select(configItem => new ConfigStructItem(((ConfigStructItem)configItem).Key, ((ConfigStructItem)configItem).Id, [], ((ConfigStructItem)configItem).Suffix) { OperationType = OperationType.Deleted }).ToList();
        var deletedStringsCopy = deletedStrings.Select(configItem => new ConfigStringItem(((ConfigStringItem)configItem).Key, ((ConfigStringItem)configItem).Id, null) { OperationType = OperationType.Deleted }).ToList();

        changesHistory.AddRange(deletedStructsCopy);
        changesHistory.AddRange(deletedStringsCopy);

        return operationType;
    }

    private static OperationType MergeConfigStruct(List<ConfigItem<object>> originalConfigValues, List<ConfigItem<object>> changesHistory, ConfigStructItem modifiedStructItem)
    {
        // if struct is modified, build changes history for it
        if (originalConfigValues.FirstOrDefault(configItem => configItem is ConfigStructItem originalConfigStruct && originalConfigStruct.Id == modifiedStructItem.Id) is ConfigStructItem originalStruct)
        {
            var newMergedStruct = new ConfigStructItem(modifiedStructItem.Key, modifiedStructItem.Id, [], modifiedStructItem.Suffix);
            var mergeOperationTypeResult = BuildChangesHistory(originalStruct.Value, modifiedStructItem.Value, newMergedStruct.Value);

            // ReSharper disable once InvertIf
            if (mergeOperationTypeResult != OperationType.Unchanged)
            {
                changesHistory.Add(newMergedStruct);
            }

            return mergeOperationTypeResult;
        }

        // if struct is new, add as is
        modifiedStructItem.OperationType = OperationType.Added;
        changesHistory.Add(modifiedStructItem);

        return OperationType.Added;
    }

    private static OperationType MergeConfigString(List<ConfigItem<object>> originalConfigValues, List<ConfigItem<object>> changesHistory, ConfigStringItem modifiedStringItem)
    {
        var originalValue = originalConfigValues.FirstOrDefault(configItem => configItem is ConfigStringItem && configItem.Key == modifiedStringItem.Key);

        if (originalValue != null && Equals(originalValue.Value, modifiedStringItem.Value))
        {
            return OperationType.Unchanged;
        }

        modifiedStringItem.OperationType = originalValue != null ? OperationType.Modified : OperationType.Added;
        changesHistory.Add(modifiedStringItem);

        return modifiedStringItem.OperationType;
    }

    private static void ApplyChangesHistory(List<ConfigItem<object>> originalConfigValues, List<ConfigItem<object>> changesHistory)
    {
        foreach (var change in changesHistory)
        {
            switch (change)
            {
                case ConfigStructItem changeStruct:
                    ApplyStructChange(originalConfigValues, changeStruct);
                    break;
                case ConfigStringItem changeString:
                    ApplyStringChange(originalConfigValues, changeString);
                    break;
                default:
                    throw new Exception($"Unknown type: {change.GetType()}");
            }
        }
    }

    private static void ApplyStructChange(List<ConfigItem<object>> originalValues, ConfigStructItem changeStruct)
    {
        var originalStructIndex = originalValues
            .Select((configItem, index) => new { ConfigItem = configItem, Index = index })
            .FirstOrDefault(x => x.ConfigItem is ConfigStructItem originalConfigStruct && originalConfigStruct.Id == changeStruct.Id);

        if (originalStructIndex?.ConfigItem is ConfigStructItem originalStruct)
        {
            // changes history is sorted by mods load order from highest to lowest, if original value has been modified by previous mod, skip it
            if (originalStruct.OperationType != OperationType.Unchanged)
            {
                return;
            }

            if (changeStruct.OperationType == OperationType.Deleted)
            {
                originalValues.RemoveAt(originalStructIndex.Index);
            }
            else
            {
                ApplyChangesHistory(originalStruct.Value, changeStruct.Value);
            }
        }
        else if (changeStruct.OperationType == OperationType.Added)
        {
            originalValues.Add(changeStruct);
        }
    }

    private static void ApplyStringChange(List<ConfigItem<object>> originalConfigValues, ConfigStringItem changeString)
    {
        var originalStringIndex = originalConfigValues
            .Select((configItem, index) => new { ConfigItem = configItem, Index = index })
            .FirstOrDefault(x => x.ConfigItem is ConfigStringItem originalConfigString && originalConfigString.Id == changeString.Id);

        if (originalStringIndex?.ConfigItem is ConfigStringItem originalString)
        {
            // changes history is sorted by mods load order from highest to lowest, if original value has been modified by previous mod, skip it
            if (originalString.OperationType != OperationType.Unchanged)
            {
                return;
            }

            if (changeString.OperationType == OperationType.Deleted)
            {
                originalConfigValues.RemoveAt(originalStringIndex.Index);
            }
            else
            {
                var updatedStringItem = new ConfigStringItem(originalString.Key, originalString.Id, changeString.Value)
                {
                    OperationType = changeString.OperationType
                };

                originalConfigValues[originalStringIndex.Index] = updatedStringItem;
            }
        }
        else if (changeString.OperationType == OperationType.Added)
        {
            originalConfigValues.Add(changeString);
        }
    }
}