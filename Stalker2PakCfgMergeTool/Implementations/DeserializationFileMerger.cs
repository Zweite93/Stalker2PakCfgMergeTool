using Stalker2PakCfgMergeTool.Entities;
using Stalker2PakCfgMergeTool.Interfaces;
using System.Collections.Generic;

namespace Stalker2PakCfgMergeTool.Implementations;

public class DeserializationFileMerger : IFileMerger
{
    public (string originalText, string mergedText) Merge(string originalText, List<string> modifiedTexts)
    {
        //var originalConfig = ConfigSerializer.Deserialize(originalText);
        //var modifiedConfigs = modifiedTexts.Select(ConfigSerializer.Deserialize).ToList();
        //var changesHistoryList = new List<Config>();

        //foreach (var modifiedConfig in modifiedConfigs)
        //{
        //    if (modifiedConfig.Values.Count == 0)
        //    {
        //        continue;
        //    }

        //    var changesHistory = new Config { Name = modifiedConfig.Name, PakName = Constants.MergedPakBaseName, Values = [] };
        //    BuildChangesHistory(originalConfig.Values, modifiedConfig.Values, changesHistory.Values);

        //    changesHistoryList.Add(changesHistory);
        //}

        //originalText = ConfigSerializer.Serialize(originalConfig);
        //var mergedText = ConfigSerializer.Serialize(changesHistory);

        //return (originalText, mergedText);

        throw new NotImplementedException();
    }

    private static OperationType BuildChangesHistory(List<ConfigKeyValuePair<object?>> originalConfigValues, List<ConfigKeyValuePair<object?>> modifiedConfigValues, List<ConfigKeyValuePair<object?>> changesHistory)
    {
        var operationType = OperationType.Unchanged;
        var keysInChangesHistory = new HashSet<string>();

        foreach (var modifiedValue in modifiedConfigValues)
        {
            //if (string.Equals(modifiedValue.Key, "ActivateFeedbackRadius"))
            //{

            //}

            OperationType mergeOperationTypeResult;
            switch (modifiedValue.Value)
            {
                case ConfigStruct modifiedStruct:
                    mergeOperationTypeResult = MergeConfigStruct(originalConfigValues, changesHistory, modifiedStruct);
                    keysInChangesHistory.Add(modifiedStruct.Id);
                    break;
                case string:
                case null:
                    mergeOperationTypeResult = MergeConfigKeyValuePair(originalConfigValues, changesHistory, modifiedValue);
                    keysInChangesHistory.Add(modifiedValue.Key);
                    break;
                default:
                    throw new Exception($"Unknown type: {modifiedValue.Value.GetType()}");
            }

            if (mergeOperationTypeResult != OperationType.Unchanged)
            {
                operationType = OperationType.Modified;
            }
        }

        // find deleted values
        var deleted = originalConfigValues.Where(originalValue =>
        (originalValue.Value is string or null && !keysInChangesHistory.Contains(originalValue.Key) ||
        originalValue.Value is ConfigStruct modifiedStruct && keysInChangesHistory.Contains(modifiedStruct.Id)) &&
        originalValue.OperationType != OperationType.Deleted)
        .Select(originalValue => new ConfigKeyValuePair<object?>(originalValue.Key, null) { OperationType = OperationType.Deleted })
        .ToList();

        // ReSharper disable once InvertIf
        if (deleted.Count > 0)
        {
            changesHistory.AddRange(deleted);
            operationType = OperationType.Modified;
        }

        return operationType;
    }

    private static OperationType MergeConfigStruct(List<ConfigKeyValuePair<object?>> originalConfigValues, List<ConfigKeyValuePair<object?>> changesHistory, ConfigStruct modifiedStruct)
    {        
        //if (modifiedValue is { Key: "InteractionEffectPrototypeSIDs", Parent.Key: "ChemicalAnomaly" })   
        //{
        //}

        var originalStruct = originalConfigValues.FirstOrDefault(kvp => kvp.Value is ConfigStruct originalConfigStruct && originalConfigStruct.Id == modifiedStruct.Id)?.Value as ConfigStruct;

        if (originalStruct != null && changesHistory.FirstOrDefault(kvp => kvp.Value is ConfigStruct mergedConfigStruct && mergedConfigStruct.Id == modifiedStruct.Id)?.Value is ConfigStruct)
        {
            // this shouldn't happen in theory

            //BuildChangesHistory(originalStruct.Values, modifiedStruct.Values, mergedStruct.Values);
            throw new Exception("Unexpected state");
        }

        if (originalStruct != null)
        {
            var newMergedStruct = new ConfigStruct(modifiedStruct.Key, modifiedStruct.Id, modifiedStruct.Suffix);
            var mergeOperationTypeResult = BuildChangesHistory(originalStruct.Value, modifiedStruct.Value, newMergedStruct.Value);

            // ReSharper disable once InvertIf
            if (mergeOperationTypeResult != OperationType.Unchanged)
            {
                var newMergedValue = new ConfigKeyValuePair<object?>(modifiedStruct.Key, newMergedStruct);
                changesHistory.Add(newMergedValue);
            }

            return mergeOperationTypeResult;
        }

        var mergedValue = new ConfigKeyValuePair<object?>(modifiedStruct.Key, modifiedStruct) { OperationType = OperationType.Added };
        changesHistory.Add(mergedValue);

        return OperationType.Added;
    }

    private static OperationType MergeConfigKeyValuePair(List<ConfigKeyValuePair<object?>> originalConfigValues, List<ConfigKeyValuePair<object?>> changesHistory, ConfigKeyValuePair<object?> modifiedValue)
    {
        var originalValue = originalConfigValues.FirstOrDefault(v => v.Key == modifiedValue.Key);
        //if (originalValue != null && originalValue.OperationType != OperationType.Unchanged)
        //{
        //    return;
        //}

        var mergedValue = changesHistory.FirstOrDefault(v => v.Key == modifiedValue.Key);
        //if (mergedValue != null && mergedValue.OperationType != OperationType.Unchanged)
        //{
        //    return;
        //}

        if (Equals(originalValue?.Value, modifiedValue.Value)) 
        {
            return OperationType.Unchanged;
        }

        if (originalValue != null && mergedValue != null)
        {
            // this shouldn't happen in theory

            //mergedValue.Value = modifiedValue.Value;
            //mergedValue.OperationType = operationType;
            throw new Exception("Unexpected state");
        }

        if (originalValue != null)
        {
            var newMergedValue = new ConfigKeyValuePair<object?>(modifiedValue.Key, modifiedValue) { OperationType = OperationType.Modified };
            changesHistory.Add(newMergedValue);

            return OperationType.Modified;
        }

        modifiedValue.OperationType = OperationType.Added;

        changesHistory.Add(modifiedValue);

        return OperationType.Added;
    }
}