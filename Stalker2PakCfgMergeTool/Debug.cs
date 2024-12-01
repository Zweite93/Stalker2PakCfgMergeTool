using Stalker2PakCfgMergeTool.Entities;

namespace Stalker2PakCfgMergeTool;

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

    public static Dictionary<string, string> FolderPaks = new()
    {
        ["test"] = "D:\\Games\\Steam Games\\steamapps\\common\\S.T.A.L.K.E.R. 2 Heart of Chornobyl\\Stalker2\\Content\\Paks\\~mods\\test",
        ["test2"] = "D:\\Games\\Steam Games\\steamapps\\common\\S.T.A.L.K.E.R. 2 Heart of Chornobyl\\Stalker2\\Content\\Paks\\~mods\\test2",
        //["test3"] = "D:\\Games\\Steam Games\\steamapps\\common\\S.T.A.L.K.E.R. 2 Heart of Chornobyl\\Stalker2\\Content\\Paks\\~mods\\test3",
        //["test4"] = "D:\\Games\\Steam Games\\steamapps\\common\\S.T.A.L.K.E.R. 2 Heart of Chornobyl\\Stalker2\\Content\\Paks\\~mods\\test4",
        //["test5"] = "D:\\Games\\Steam Games\\steamapps\\common\\S.T.A.L.K.E.R. 2 Heart of Chornobyl\\Stalker2\\Content\\Paks\\~mods\\test5",
        //["test6"] = "D:\\Games\\Steam Games\\steamapps\\common\\S.T.A.L.K.E.R. 2 Heart of Chornobyl\\Stalker2\\Content\\Paks\\~mods\\test6"
    };

    public static bool ExportToFolder = true;

    public static bool MergeWithoutConflict = true;

    public static async Task ExportMergeToFolder(string folderName, string path, List<PakFileWithContent> pakFiles)
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
        }
    }
}
