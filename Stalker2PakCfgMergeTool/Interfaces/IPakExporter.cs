using Stalker2PakCfgMergeTool.Entities;

namespace Stalker2PakCfgMergeTool.Interfaces;

public interface IPakExporter
{
    void CreatePak(string pakName, string path, List<PakFileWithContent> pakFiles);
}