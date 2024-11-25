using Stalker2PakCfgMergeTool.Entities;

namespace Stalker2PakCfgMergeTool.Interfaces;

public interface IPakProvider : IDisposable
{
    List<Pak> GetPaksInfo();

    Task<string> LoadPakFile(string pakFilePath, string? pakName = null);
}