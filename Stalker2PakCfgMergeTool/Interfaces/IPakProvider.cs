using Stalker2PakCfgMergeTool.Entities;
using Stalker2PakCfgMergeTool.Enums;

namespace Stalker2PakCfgMergeTool.Interfaces;

public interface IPakProvider : IDisposable
{
    List<Pak> GetPaksInfo(PakSearchOption pakSearchOption);

    Task<string> LoadPakFile(string pakFilePath, PakSearchOption pakSearchOption, string? pakName = null);
}