using System.Diagnostics;
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
    private const string Win64ExeFilePath = "Stalker2/Binaries/Win64/Stalker2-Win64-Shipping.exe";
    private const string WinGdkExeFilePath = "Content/Stalker2/Binaries/WinGDK/Stalker2-WinGDK-Editor.exe";

    private static string? _exeFilePath;

    public static async Task Main(string[] args)
    {
        var gamePath = args.FirstOrDefault() ?? "";

        if (string.IsNullOrEmpty(gamePath) || !Directory.Exists(gamePath))
        {
            ShowInvalidPathMessage();
            return;
        }

        if (File.Exists(Path.Combine(gamePath, Win64ExeFilePath)))
        {
            _exeFilePath = Win64ExeFilePath;
        }
        else if (File.Exists(Path.Combine(gamePath, WinGdkExeFilePath)))
        {
            _exeFilePath = WinGdkExeFilePath;
        }
        else
        {
            ShowInvalidPathMessage();
            return;
        }

        var aesKey = AesKeyGetter.Get(Path.Combine(gamePath, _exeFilePath)) ?? throw new Exception("AES key not found");


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

        Console.WriteLine($"Merge pak created: {mergedPakName}\n");

        Console.WriteLine("Open up the summary.html file in your browser to see the merge results? [y/n]\n");

        if (Console.ReadKey().KeyChar == 'y')
        {
            var summaryFilePath = Path.Combine(Directory.GetCurrentDirectory(), "diff.html");
            var processStartInfo = new ProcessStartInfo
            {
                FileName = summaryFilePath,
                UseShellExecute = true
            };
            Process.Start(processStartInfo);
        }
    }

    private static void ShowInvalidPathMessage()
    {
        Console.WriteLine("The game executable file not found in the provided path.");
        Console.WriteLine("Please provide the path to the game folder as the first argument.\n");
        Console.WriteLine("Example: Stalker2PakCfgMergeTool.exe \"D:\\Games\\Steam Games\\steamapps\\common\\S.T.A.L.K.E.R. 2 Heart of Chornobyl\"\n");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}