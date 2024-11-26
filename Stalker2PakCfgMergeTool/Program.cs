using Stalker2PakCfgMergeTool.Implementations;

namespace Stalker2PakCfgMergeTool;

public static class Constants
{
    public const string MergedPakBaseName = "merged_cfg_modpack";
}

public class Program
{
    private const string PaksDirectory = "Stalker2/Content/Paks";
    private const string ModsDirectoryName = "~mods";
    private const string ReferencePakName = "pakchunk0-Windows.pak";
    private const string ExeFilePath = "Stalker2/Binaries/Win64/Stalker2-Win64-Shipping.exe";

    public static async Task Main(string[] args)
    {
        var gamePath = args.FirstOrDefault() ?? "";

        if (!File.Exists(Path.Combine(gamePath, ExeFilePath)))
        {
            Console.WriteLine("The game executable file not found in the provided path.");
            Console.WriteLine("Please provide the path to the game folder as the first argument.\n");
            Console.WriteLine("Example: Stalker2PakCfgMergeTool.exe \"D:\\Games\\Steam Games\\steamapps\\common\\S.T.A.L.K.E.R. 2 Heart of Chornobyl\"\n");
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
            return;
        }

        var aesKey = AesKeyGetter.Get(Path.Combine(gamePath, ExeFilePath)) ?? throw new Exception("AES key not found");


        Console.WriteLine($"\nYour AES key: {aesKey}\n");

        var modsPath = Path.Combine(gamePath, PaksDirectory, ModsDirectoryName);

        using var pakMerger = new PakMerger(
            new Cue4PakProvider(modsPath, aesKey),
            new Cue4PakProvider(Path.Combine(gamePath, PaksDirectory), aesKey, ReferencePakName));

        var mergedPakFiles = await pakMerger.MergePaksWithConflicts();

        if (mergedPakFiles.Count == 0)
        {
            Console.WriteLine("No conflicts found.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
            return;
        }

        var filePrefix = "zzz";

        var dirInfo = new DirectoryInfo(modsPath);
        var lastModPakFileName = dirInfo.GetFiles("*.pak")
            .Select(f => f.Name)
            .Where(name => !name.Contains(Constants.MergedPakBaseName))
            .Order()
            .LastOrDefault();

        if (!string.IsNullOrEmpty(lastModPakFileName))
        {
            var lastModPakName = Path.GetFileNameWithoutExtension(lastModPakFileName);
            var zCount = lastModPakName.TakeWhile(c => c == 'z').Count();
            filePrefix = new string('z', zCount + 1);
        }

        var currentDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var mergedPakName = $"{filePrefix}_{Constants.MergedPakBaseName}_{currentDate}_P.pak";
        var mergedPakPath = Path.Combine(gamePath, PaksDirectory, ModsDirectoryName, mergedPakName);

        var pakCreator = new NetPakCreator();
        pakCreator.CreatePak(Constants.MergedPakBaseName, mergedPakPath, mergedPakFiles);
        ;

        Console.WriteLine("Press any key to exit...");
        Console.ReadLine();
    }
}