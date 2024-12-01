using Stalker2PakCfgMergeTool.Entities;
using Stalker2PakCfgMergeTool.Implementations;

namespace Stalker2PakCfgMergeTool.Interfaces;

public interface IConfigSerializerVerifier
{
    VerificationResult Verify(Config config, string configText);
}