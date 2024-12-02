using System.Diagnostics;
using System.Text.RegularExpressions;
using Stalker2PakCfgMergeTool.Implementations;

namespace Stalker2PakCfgMergeTool;

public partial class Program
{
    private const string PaksDirectory = "Stalker2/Content/Paks";
    private const string ModsDirectoryName = "~mods";

    public static async Task Main(string[] args)
    {
        var gamePath = "";
        string? aesKey = null;
        var skipReport = false;
        var unpack = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--aes":
                    if (i + 1 < args.Length)
                    {
                        aesKey = args[i + 1];
                        i++;
                    }
                    else
                    {
                        Console.WriteLine("Error: Missing value for --aes option.");
                        PressAnyKeyToExit();
                        return;
                    }
                    break;
                case "--skipreport":
                    skipReport = true;
                    break;
                case "--unpack":
                    unpack = true;
                    break;
                default:
                    if (string.IsNullOrEmpty(gamePath))
                    {
                        gamePath = args[i];
                    }
                    else
                    {
                        Console.WriteLine($"Error: Unknown argument '{args[i]}'.");
                        PressAnyKeyToExit();
                        return;
                    }
                    break;
            }
        }

        if (string.IsNullOrEmpty(gamePath))
        {
            Console.WriteLine("Error: Missing required game path argument.");
            PressAnyKeyToExit();
            return;
        }

        try
        {
            await Execute(gamePath, aesKey, skipReport, unpack);
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected error occurred:");
            Console.WriteLine(e.Message);
            Console.WriteLine();
            Console.WriteLine(e.StackTrace);
            Console.WriteLine();
            PressAnyKeyToExit();
        }
    }

    private static async Task Execute(string gamePath, string? aesKey, bool skipReport, bool unpack)
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

        var paksDirectory = Path.Combine(gamePath, PaksDirectory);

        aesKey = await GetValidAesKey(aesKey, gamePath, paksDirectory);

        if (aesKey == null)
        {
            Console.WriteLine("Could not find valid AES key. Please find provide AES key as an argument.\n");
            Console.WriteLine("EXAMPLE: --aes your_aes_key");
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

        var originalPaksDir = Path.Combine(gamePath, paksDirectory);

#if DEBUG
        // TODO: make a unit test out if it
        //var configSerializer = new ConfigSerializer();
        //var configSerializerVerifier = new ConfigSerializerVerifier(configSerializer);
        //var configMerger = new DeserializationFileMerger(configSerializer, configSerializerVerifier);
        //var configMergerVerifier = new ConfigMergerVerifier(configSerializer, configMerger);
        //var pakProvider = new Cue4PakProvider(originalPaksDir, originalPaksDir, aesKey);
        //var serializerTester = new SerializerTest(pakProvider, configSerializer, configSerializerVerifier, configMergerVerifier);

        ////var result = await serializerTester.Test("Stalker2");
        //var result = await serializerTester.TestMerger("Stalker2");

        //Console.WriteLine("All files are equal: " + result.allEquals + "\n");

        //return;
#endif

        var pakMerger = new PakMerger(
            new Cue4PakProvider(originalPaksDir, modsPath, aesKey),
            new DeserializationFileMerger(new ConfigSerializer(), new ConfigSerializerVerifier(new ConfigSerializer())));

        var mergedPakFiles = await pakMerger.MergePaksWithConflicts();

        pakMerger.Dispose();

        if (mergedPakFiles.Count == 0)
        {
            Console.WriteLine("No conflicts found.");
            PressAnyKeyToExit();
            return;
        }

        var dirInfo = new DirectoryInfo(modsPath);
        var loadPriority = GetLoadPriority(dirInfo);

        var mergedPakName = $"_{Constants.MergedPakBaseName}_loadPriority_{loadPriority}_P.pak";
        var mergedPakPath = Path.Combine(gamePath, paksDirectory, ModsDirectoryName, mergedPakName);

        //cleanup old merged pak files
        var oldMergedPakFiles = dirInfo.GetFiles($"_{Constants.MergedPakBaseName}_*.pak");
        foreach (var oldMergedPakFile in oldMergedPakFiles)
        {
            oldMergedPakFile.Delete();
        }

        if (unpack)
        {
            await DebugTools.Debug.ExportMergeToFolder(Path.Combine(gamePath, paksDirectory, ModsDirectoryName, "merged"), mergedPakFiles);
        }

        var pakCreator = new NetPakCreator();
        pakCreator.CreatePak(Constants.MergedPakBaseName, mergedPakPath, mergedPakFiles);

        Console.WriteLine($"Merge pak created: {mergedPakName}\n\n");

        if (!skipReport)
        {
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
    }

    private static void ShowInvalidPathMessage()
    {
        Console.WriteLine("The game executable file not found in the provided path.");
        Console.WriteLine("Please provide the path to the game folder as the first argument.\n");
        Console.WriteLine("Example: Stalker2PakCfgMergeTool.exe \"D:\\Games\\Steam Games\\steamapps\\common\\S.T.A.L.K.E.R. 2 Heart of Chornobyl\"\n");
        PressAnyKeyToExit();
    }

    private static async Task<string?> GetValidAesKey(string? aesKey, string gamePath, string paksDirectory)
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

        Console.WriteLine("Default AES key is invalid.\n");

        return null;
    }

    // games load priority is defined by this format: modName_{loadPriority}_P.pak
    private static int GetLoadPriority(DirectoryInfo dirInfo)
    {
        // find all files and directories in the mods folder
        var fileNames = dirInfo.GetFiles().Where(f => f.Name.EndsWith(".pak") && !f.Name.Contains(Constants.MergedPakBaseName)).Select(f => f.Name.Replace(".pak", string.Empty));
        var directoryNames = dirInfo.GetDirectories().Select(d => d.Name);
        var modNames = fileNames.Concat(directoryNames).ToList();

        var loadPriority = 100;
        var regex = LoadPriorityRegex();
        foreach (var modName in modNames)
        {
            var match = regex.Match(modName);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var modLoadPriority) && modLoadPriority >= loadPriority)
            {
                loadPriority = (modLoadPriority / 10 + 1) * 10;
            }
        }

        return loadPriority;
    }

    private static void PressAnyKeyToExit()
    {
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    [GeneratedRegex(@"_(\d+)_P$")]
    private static partial Regex LoadPriorityRegex();
}