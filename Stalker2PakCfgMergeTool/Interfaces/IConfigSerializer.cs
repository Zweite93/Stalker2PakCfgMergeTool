using Stalker2PakCfgMergeTool.Entities;

namespace Stalker2PakCfgMergeTool.Interfaces;

public interface IConfigSerializer
{
    string Serialize(Config config);
    Config Deserialize(string fileName, string pakName, string configText);
}