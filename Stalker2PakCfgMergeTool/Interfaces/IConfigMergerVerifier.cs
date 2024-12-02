using Stalker2PakCfgMergeTool.Implementations;

namespace Stalker2PakCfgMergeTool.Interfaces;

public interface IConfigMergerVerifier
{
    VerificationResult Verify(string fileName, string pakName, string configText);
}