using System.Diagnostics;
using Stalker2PakCfgMergeTool.Entities;
using Stalker2PakCfgMergeTool.Implementations;

namespace Stalker2PakCfgMergeTool;

public class Program
{
    private const string PaksDirectory = "Stalker2/Content/Paks";
    private const string ModsDirectoryName = "~mods";
    private const string ReferencePakName = "pakchunk0-Windows.pak";

    public static async Task Main(string[] args)
    {
        //var textToParse = await File.ReadAllTextAsync(@"C:\Users\Zweit\Desktop\text2parse.txt");
        //var lines = textToParse.Split("\n").ToArray();
        //lines = lines.Where(l => !l.TrimStart().StartsWith("//") && !string.IsNullOrWhiteSpace(l)).Select(l => l.Replace("\t", "   ")).ToArray();
        //var textToCompare = string.Join("\n", lines);

        //var obj = ConfigSerializer.Deserialize(textToParse);
        //var result = ConfigSerializer.Serialize(obj);

        //var equal = textToCompare.Trim() == result.Trim();

        //var textToParse2 = string.Join('\n', textToCompare.Split("\n").Select(l => l.Trim()));
        //var result2 = string.Join('\n', result.Split("\n").Select(l => l.Trim()));

        //var equal2 = textToCompare.Trim() == result.Trim();

        //return;

        var gamePath = args.FirstOrDefault() ?? "";
        var aesKey = args.Skip(1).FirstOrDefault();

        try
        {
            await Execute(gamePath, aesKey);
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected error occurred:");
            Console.WriteLine(e.Message);
            Console.WriteLine();
            PressAnyKeyToExit();
        }
    }

    private static async Task Execute(string gamePath, string? aesKey)
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

        aesKey = await GetValidAesKey(aesKey, exeFilePath, gamePath, paksDirectory);

        if (aesKey == null)
        {
            Console.WriteLine("Could not find valid AES key. You can try getting your oun AES key and passing it as a second argument after game root path.");
            PressAnyKeyToExit();
            return;
        }

        Console.WriteLine("AES key is valid.\n");

        var modsPath = Path.Combine(gamePath, paksDirectory, ModsDirectoryName);
        Console.WriteLine($"Mods path: {modsPath}\n");

        if (!Directory.Exists(modsPath))
        {
            Console.WriteLine("Mods folder does not exist. What are you trying to merge then?\n");
            PressAnyKeyToExit();
            return;
        }

        var serializerTester = new SerializerTest(new Cue4PakProvider(Path.Combine(gamePath, paksDirectory), aesKey, ReferencePakName));
        var allEquals = await serializerTester.Test("Stalker2");

        Console.WriteLine("All files are equal: " + allEquals + "\n");

        return;


        var pakMerger = new PakMerger(
            new Cue4PakProvider(modsPath, aesKey),
            new Cue4PakProvider(Path.Combine(gamePath, paksDirectory), aesKey, ReferencePakName),
            new DeserializationFileMerger());

        var mergedPakFiles = await pakMerger.MergePaksWithConflicts();

        pakMerger.Dispose();

        if (mergedPakFiles.Count == 0)
        {
            Console.WriteLine("No conflicts found.");
            PressAnyKeyToExit();
            return;
        }

        var filePrefix = "zzz";

        var dirInfo = new DirectoryInfo(modsPath);

        var lastModPakFileName = dirInfo.GetFiles("*.pak")
            .Select(f => f.Name)
            .Where(name => !name.Contains(Constants.MergedPakBaseName))
            .Order()
            .LastOrDefault()?
            .ToLower();

        var lastModFolderName = dirInfo.GetDirectories()
            .Select(di => di.Name)
            .Where(name => !name.Contains(Constants.MergedPakBaseName))
            .Order()
            .LastOrDefault()?
            .ToLower();

        if (!string.IsNullOrEmpty(lastModPakFileName) || !string.IsNullOrEmpty(lastModFolderName))
        {
            var fileZCount = lastModPakFileName?.TakeWhile(c => c == 'z').Count();
            var folderZCount = lastModFolderName?.TakeWhile(c => c == 'z').Count();
            var zCount = Math.Max(fileZCount ?? 0, folderZCount ?? 0);

            filePrefix = new string('z', zCount + 1);
        }

        var currentDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var mergedPakName = $"{filePrefix}_{Constants.MergedPakBaseName}_{currentDate}_P.pak";
        var mergedPakPath = Path.Combine(gamePath, paksDirectory, ModsDirectoryName, mergedPakName);

        if (Debug.IsDebug && Debug.ExportToFolder)
        {
            await Debug.ExportMergeToFolder(mergedPakName, Path.Combine(gamePath, paksDirectory, ModsDirectoryName, "merged"), mergedPakFiles);
        }
        else
        {
            var pakCreator = new NetPakCreator();
            pakCreator.CreatePak(Constants.MergedPakBaseName, mergedPakPath, mergedPakFiles);
        }

        Console.WriteLine($"Merge pak created: {mergedPakName}\n\n");

        Console.WriteLine("Open up the Diff Viewer? [y/n]\n");

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
        path = Path.Combine(path, "Stalker2/Binaries");

        // some users reported that they have both Win64 and WinGDK folders, find exe file in either one
        var exeFile = FindExe("Win64") ?? FindExe("WinGDK") ?? throw new Exception($"The game executable file not found. Provided binaries folder: {path}");

        return exeFile;

        string? FindExe(string winVersion)
        {
            var shippingExe = $"Stalker2-{winVersion}-Shipping.exe";
            var editorExe = $"Stalker2-{winVersion}-Editor.exe";

            var shippingExePath = Path.Combine(path, winVersion, shippingExe);
            var editorExePath = Path.Combine(path, winVersion, editorExe);

            if (File.Exists(shippingExePath))
            {
                return shippingExePath;
            }

            if (File.Exists(editorExePath))
            {
                return editorExePath;
            }

            return null;
        }
    }

    private static void ShowInvalidPathMessage()
    {
        Console.WriteLine("The game executable file not found in the provided path.");
        Console.WriteLine("Please provide the path to the game folder as the first argument.\n");
        Console.WriteLine("Example: Stalker2PakCfgMergeTool.exe \"D:\\Games\\Steam Games\\steamapps\\common\\S.T.A.L.K.E.R. 2 Heart of Chornobyl\"\n");
        PressAnyKeyToExit();
    }

    private static async Task<string?> GetValidAesKey(string? aesKey, string exeFilePath, string gamePath, string paksDirectory)
    {
        const string defaultAecKey = "33A604DF49A07FFD4A4C919962161F5C35A134D37EFA98DB37A34F6450D7D386";

        if (!string.IsNullOrEmpty(aesKey))
        {
            if (await Cue4PakProvider.TestAesKey(aesKey, Path.Combine(gamePath, paksDirectory)))
            {
                return aesKey;
            }

            Console.WriteLine("Provided AES key is invalid. Trying the default AES key.\n");
        }

        if (await Cue4PakProvider.TestAesKey(defaultAecKey, Path.Combine(gamePath, paksDirectory)))
        {
            return defaultAecKey;
        }

        Console.WriteLine("Default AES key is invalid. Trying to grab AES key from game's executable.\n");

        aesKey = AesKeyGetter.Get(Path.Combine(gamePath, exeFilePath));

        if (string.IsNullOrEmpty(aesKey))
        {
            Console.WriteLine("AES key not found in game's executable.\n");
            return null;
        }

        if (await Cue4PakProvider.TestAesKey(aesKey, Path.Combine(gamePath, paksDirectory)))
        {
            return aesKey;
        }

        Console.WriteLine("AES key from game's executable is invalid.\n");

        return null;
    }

    private static void PressAnyKeyToExit()
    {
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}