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

    public static async Task Main(string[] args)
    {
        var gamePath = args.FirstOrDefault() ?? "";

        try
        {
            await Execute(gamePath);
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected error occurred:");
            Console.WriteLine(e.Message);
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    private static async Task Execute(string gamePath)
    {
        if (string.IsNullOrEmpty(gamePath) || !Directory.Exists(gamePath))
        {
            ShowInvalidPathMessage();
            return;
        }

        var gameRootDirInfo = new DirectoryInfo(gamePath);

        // game pass version has Content folder in the root directory
        if (gameRootDirInfo.GetDirectories().Select(di => di.Name).Contains("Content"))
        {
            gamePath = Path.Combine(gamePath, "Content");
        }

        var exeFilePath = GetExeFilePath(gamePath);
        var paksDirectory = Path.Combine(gamePath, PaksDirectory);

        var aesKey = AesKeyGetter.Get(Path.Combine(gamePath, exeFilePath)) ?? throw new Exception("AES key not found");

        Console.WriteLine($"\nYour AES key: {aesKey}\n");

        var modsPath = Path.Combine(gamePath, paksDirectory, ModsDirectoryName);

        using var pakMerger = new PakMerger(
            new Cue4PakProvider(modsPath, aesKey),
            new Cue4PakProvider(Path.Combine(gamePath, paksDirectory), aesKey, ReferencePakName));

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
        var mergedPakPath = Path.Combine(gamePath, paksDirectory, ModsDirectoryName, mergedPakName);

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

    private static string GetExeFilePath(string path)
    {
        const string win64 = "Win64";
        const string winGdk = "WinGDK";
        const string binariesDirectory = "Stalker2/Binaries";
        const string shipping = "Shipping";
        const string editor = "Editor";

        path = Path.Combine(path, binariesDirectory);

        string win;
        var directories = new DirectoryInfo(path).GetDirectories().Select(di => di.Name).ToList();

        if (directories.Contains(win64))
        {
            win = win64;
        }
        else if (directories.Contains(winGdk))
        {
            win = winGdk;
        }
        else
        {
            throw new Exception($"{path} path does not contain Win64 or WinGDK directory.");
        }

        path = Path.Combine(path, win);
        var exeFile = $"Stalker2-{win}";

        var shippingExePath = Path.Combine(path, $"{exeFile}-{shipping}.exe");
        var editorExePath = Path.Combine(path, $"{exeFile}-{editor}.exe");

        if (File.Exists(shippingExePath))
        {
            return Path.Combine(path, shippingExePath);
        }

        if (File.Exists(editorExePath))
        {
            return Path.Combine(path, editorExePath);
        }

        throw new Exception($"The game executable file not found in the provided path: {path}");
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