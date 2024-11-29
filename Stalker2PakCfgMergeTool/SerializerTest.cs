using Stalker2PakCfgMergeTool.Interfaces;

namespace Stalker2PakCfgMergeTool;

public class SerializerTest
{
    private static readonly object Lock = new();
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

        var allFiles = await LoadAllFilesInParallel(configFiles);
        var allEquals = true;

        var cts = new CancellationTokenSource();
        var options = new ParallelOptions { CancellationToken = cts.Token };

        try
        {
            Parallel.ForEach(allFiles, options, (textToParse, state) =>
            {
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
                            cts.Cancel();
                            state.Stop();
                            allEquals = false;
                            return;
                        }

                        for (var i = 0; i < linesToCompare.Length; i++)
                        {
                            var line = linesToCompare[i];
                            var resultLine = resultLines[i];
                            if (resultLine != line)
                            {
                                cts.Cancel();
                                state.Stop();
                                allEquals = false;
                                return;
                            }
                        }

                        return;
                    }
                }

                Interlocked.Increment(ref filesTested);

                if (numberOfFilesToTest > 0 && filesTested >= numberOfFilesToTest)
                {
                    cts.Cancel();
                    state.Stop();
                }
            });
        }
        catch (OperationCanceledException)
        {
            return allEquals;
        }

        return allEquals;
    }

    private async Task<List<string>> LoadAllFilesInParallel(IEnumerable<string> pakFileKeys)
    {
        var tasks = pakFileKeys.Select(async key =>
        {
            var text = await _provider.LoadPakFile(key);
            return text;
        });

        return (await Task.WhenAll(tasks)).ToList();
    }
}