using Stalker2PakCfgMergeTool.Interfaces;
using System.Text.RegularExpressions;

namespace Stalker2PakCfgMergeTool;

public class SerializerTest
{
    // Files or folders with files with known format issues
    private static readonly List<string> DirsToSkip = ["Stalker2\\Content\\GameLite\\GameData\\Scripts\\OnGameLaunch"];
    private static readonly List<string> FilesToSkip = ["QuestArtifactPrototypes.cfg", "RelationPrototypes.cfg", "VortexDudeItemGenerator.cfg", "GenericLairPrototypes.cfg"];

    private static readonly object Lock = new();
    private readonly IPakProvider _provider;

    public SerializerTest(IPakProvider provider)
    {
        _provider = provider;
    }

    public async Task<(bool allTrue, int filesWithMissingLines, int filesWithDifferentLines)> Test(string pakSubfolderPath, int? numberOfFilesToTest = null)
    {
        var pak = _provider.GetPaksInfo().First();

        var filesTested = 0;
        var configFiles = pak.PakFileKeys.Where(key => key.StartsWith(pakSubfolderPath) && key.EndsWith(".cfg") && !DirsToSkip.Contains(Path.GetDirectoryName(key)!) && !FilesToSkip.Contains(Path.GetFileName(key)));

        if (numberOfFilesToTest.HasValue)
        {
            configFiles = configFiles.Take(numberOfFilesToTest.Value);
        }

        var allFiles = await LoadAllFilesInParallel(configFiles);
        var allEquals = true;


        var filesWithMissingLines = 0;
        var filesWithDifferentLines = 0;

        Parallel.ForEach(allFiles, (file, state) =>
        {
            var fileKey = file.Item1;
            var textToParse = file.Item2;
            var directory = Path.GetDirectoryName(fileKey)!;

            var lines = textToParse.Split("\n").ToArray();
            lines = lines.Where(l => !l.TrimStart().StartsWith("//") && !string.IsNullOrWhiteSpace(l)).Select(l => l.Replace("\t", "   ")).ToArray();
            var textToCompare = string.Join("\n", lines);


            var obj = ConfigSerializer.Deserialize(textToParse);
            var result = ConfigSerializer.Serialize(obj);

            var equal = result.Trim() == textToCompare.Trim();

            if (!equal)
            {
                lock (Lock)
                {
                    var linesToCompare = textToCompare.Trim().Split("\n").ToArray();
                    var resultLines = result.Trim().Split("\n").ToArray();

                    if (linesToCompare.Length != resultLines.Length)
                    {
                        allEquals = false;
                        filesWithMissingLines++;
                        return;

                    }

                    for (var i = 0; i < linesToCompare.Length; i++)
                    {
                        var line = Normalize(linesToCompare[i]);
                        var resultLine = Normalize(resultLines[i]);
                        if (resultLine != line)
                        {
                            allEquals = false;
                            filesWithDifferentLines++;
                            return;
                        }
                    }
                }
            }
        });


        return (allEquals, filesWithMissingLines, filesWithDifferentLines);
    }

    private async Task<List<(string, string)>> LoadAllFilesInParallel(IEnumerable<string> pakFileKeys)
    {
        var tasks = pakFileKeys.Select(async key =>
        {
            var test = Path.GetDirectoryName(key);
            var text = await _provider.LoadPakFile(key);
            return (key, text);
        });

        return (await Task.WhenAll(tasks)).ToList();
    }

    private static string Normalize(string input)
    {
        input = input.Trim().Replace("  ", " ");

        var separators = new List<char> { ':', '=' };
        foreach (var separator in separators)
        {
            input = Regex.Replace(input, $@"\s*{separator}\s*", $" {separator} ");
        }

        // Normalize whitespace around '{' and '}'
        input = Regex.Replace(input, @"\s*{\s*", " { ");
        input = Regex.Replace(input, @"\s*}\s*", " } ");

        return input;
    }
}