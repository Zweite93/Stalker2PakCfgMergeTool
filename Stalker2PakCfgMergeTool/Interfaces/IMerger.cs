namespace Stalker2PakCfgMergeTool.Interfaces;

public interface IFileMerger
{
    (string originalText, string mergedText) Merge(string originalText, string fileName, List<(string pakName, string modifiedText)> modifiedTexts);
}