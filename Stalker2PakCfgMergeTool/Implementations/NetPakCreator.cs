using Stalker2PakCfgMergeTool.Entities;
using Stalker2PakCfgMergeTool.Interfaces;

namespace Stalker2PakCfgMergeTool.Implementations;

public class NetPakCreator : IPakExporter
{
    public void CreatePak(string pakName, string path, List<PakFileWithContent> pakFiles)
    {
        var fileName = new NetPak.FString(pakName);
        var mountPoint = new NetPak.FString("../../../");

        using var pakFile = NetPak.PakFile.Create(fileName, mountPoint);

        foreach (var fileEntry in pakFiles)
        {
            var fileEntryPath = new NetPak.FString(fileEntry.FilePath);
            pakFile.AddEntry(fileEntryPath, fileEntry.Content);
        }

        pakFile.Save(path);
    }
}