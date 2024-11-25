namespace Stalker2PakCfgMergeTool.Entities;

public class FileConflict
{
    public required string FileName { get; set; }
    public required string FilePath { get; set; }
    public required List<PakFile> ConflictWith { get; set; }
}