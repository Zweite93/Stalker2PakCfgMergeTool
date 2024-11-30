namespace Stalker2PakCfgMergeTool;

[Flags]
public enum OperationType
{
    Unchanged = 1,
    Modified = 2,
    Added = 4,
    Deleted = 8
}