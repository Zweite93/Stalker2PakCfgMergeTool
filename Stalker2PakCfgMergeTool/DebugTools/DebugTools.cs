using Stalker2PakCfgMergeTool.Entities;

namespace Stalker2PakCfgMergeTool.DebugTools;

internal static class Debug
{
    public static bool IsDebug
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }

    public static List<string> Paks = [];

    public static Dictionary<string, string> FolderPaks = [];

    public static bool MergeWithoutConflict = false;

    public static async Task ExportMergeToFolder(string path, List<PakFileWithContent> pakFiles)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        else
        {
            Directory.Delete(path, true);
            Directory.CreateDirectory(path);
        }

        foreach (var file in pakFiles)
        {
            var filePath = Path.Combine(path, file.FilePath);
            var directoryPath = Path.GetDirectoryName(filePath)!;

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            await File.WriteAllBytesAsync(filePath, file.Content);
            await File.WriteAllBytesAsync(filePath + ".original", file.OriginalContent);
        }
    }
}
