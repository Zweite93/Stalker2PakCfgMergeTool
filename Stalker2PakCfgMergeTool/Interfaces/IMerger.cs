namespace Stalker2PakCfgMergeTool.Interfaces;

public interface IFileMerger
{
    string Merge(string originalText, List<string> modifiedTexts);
}