namespace Stalker2PakCfgMergeTool.Entities;

public class PakFile
{
    public required string PakName { get; set; }
    public required string FileName { get; set; }
    public required string FilePath { get; set; }
}

public class PakFileWithContent : PakFile
{
    public required byte[] Content { get; set; }
}