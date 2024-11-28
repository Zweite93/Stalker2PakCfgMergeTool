using Stalker2PakCfgMergeTool.Interfaces;
using System.Linq;

namespace Stalker2PakCfgMergeTool;

public class SerializerTest
{
    private readonly IPakProvider _provider;

    public SerializerTest(IPakProvider provider)
    {
        _provider = provider;
    }

    public async Task<bool> Test(string pakSubfolderPath, int? numberOfFilesToTest = null)
    {
        var pak = _provider.GetPaksInfo().First();

        var filesTested = 0;
        var configFiles = pak.PakFileKeys.Where(key => key.StartsWith(pakSubfolderPath) && key.EndsWith(".cfg"));

        if (numberOfFilesToTest.HasValue)
        {
            configFiles = configFiles.Take(numberOfFilesToTest.Value);
        }

        foreach (var pakFileKey in configFiles)
        {
            var textToParse = await _provider.LoadPakFile(pakFileKey);
            var lines = textToParse.Split("\n").ToArray();
            lines = lines.Where(l => !l.TrimStart().StartsWith("//") && !string.IsNullOrWhiteSpace(l)).ToArray();
            var textToCompare = string.Join("\n", lines);

            var obj = ConfigSerializer.Deserialize(textToParse);
            var result = ConfigSerializer.Serialize(obj);

            var equal = result.Trim() == textToCompare.Trim();

            if (!equal)
            {
                var linesToCompare = result.Split("\n").ToArray();
                var resultLines = result.Split("\n").ToArray();

                if (linesToCompare.Length != resultLines.Length)
                {
                    return false;
                }

                for (var i = 0; i < linesToCompare.Length; i++)
                {
                    if (linesToCompare[i] != resultLines[i])
                    {
                        return false;
                    }
                }
            }

            filesTested++;

            if (numberOfFilesToTest > 0 && filesTested >= numberOfFilesToTest)
            {
                break;
            }
        }

        return true;
    }
}