namespace Stalker2PakCfgMergeTool.Entities;

public class Pak
{
    public required string Name { get; set; }
    public required List<string> PakFileKeys { get; set; }
}