namespace Stalker2PakCfgMergeTool.Interfaces;

public interface IFileMerger
{
    (string originalText, string mergedText) Merge(string originalText, List<string> modifiedTexts);
}