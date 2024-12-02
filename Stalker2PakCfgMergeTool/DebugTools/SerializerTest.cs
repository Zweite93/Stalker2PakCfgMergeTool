#if DEBUG

using Stalker2PakCfgMergeTool.Entities;
using Stalker2PakCfgMergeTool.Implementations;
using Stalker2PakCfgMergeTool.Interfaces;

namespace Stalker2PakCfgMergeTool.DebugTools;

public class SerializerTest
{
    // Files or folders with files with known format issues
    private static readonly List<string> DirsToSkip = ["Stalker2\\Content\\GameLite\\GameData\\Scripts\\OnGameLaunch"];
    private static readonly List<string> FilesToSkip = ["QuestArtifactPrototypes.cfg", "RelationPrototypes.cfg", "VortexDudeItemGenerator.cfg", "GenericLairPrototypes.cfg"];

    private readonly IPakProvider _provider;
    private readonly IConfigSerializer _configSerializer;
    private readonly IConfigSerializerVerifier _configSerializerVerifier;
    private readonly IConfigMergerVerifier _configMergerVerifier;

    public SerializerTest(IPakProvider provider, IConfigSerializer configSerializer, IConfigSerializerVerifier configSerializerVerifier, IConfigMergerVerifier configMergerVerifier)
    {
        _provider = provider;
        _configSerializer = configSerializer;
        _configSerializerVerifier = configSerializerVerifier;
        _configMergerVerifier = configMergerVerifier;
    }

    public async Task<(bool allEquals, int filesWithLinesCountMismatch, int filesWithLinesContentMismatch)> Test(string pakSubfolderPath, int? numberOfFilesToTest = null)
    {
        var (pak, allFiles) = await LoadAllFilesInParallel(pakSubfolderPath, numberOfFilesToTest);

        var verifyTasks = new Task<VerificationResult>[allFiles.Count];
        for (var i = 0; i < allFiles.Count; i++)
        {
            var (fileKey, text) = allFiles[i];
            verifyTasks[i] = Task.Run(() =>
            {
                var config = _configSerializer.Deserialize(Path.GetFileName(fileKey), pak.Name, text);
                return _configSerializerVerifier.Verify(config, text);
            });
        }

        var results = await Task.WhenAll(verifyTasks);

        return GetResults(results);
    }

    public async Task<(bool allEquals, int filesWithLinesCountMismatch, int filesWithLinesContentMismatch)> TestMerger(string pakSubfolderPath, int? numberOfFilesToTest = null)
    {
        var (pak, allFiles) = await LoadAllFilesInParallel(pakSubfolderPath, numberOfFilesToTest);

        var verifyTasks = new Task<VerificationResult>[allFiles.Count];
        for (var i = 0; i < allFiles.Count; i++)
        {
            var (fileKey, text) = allFiles[i];
            verifyTasks[i] = Task.Run(() => _configMergerVerifier.Verify(Path.GetFileName(fileKey), pak.Name, text));
        }

        var results = await Task.WhenAll(verifyTasks);

        return GetResults(results);
    }

    private async Task<(Pak pak, List<(string fileKey, string text)>)> LoadAllFilesInParallel(string pakSubfolderPath, int? numberOfFilesToTest = null)
    {
        var pak = _provider.GetPaksInfo().First();

        var configFiles = pak.PakFileKeys.Where(key => key.StartsWith(pakSubfolderPath) && key.EndsWith(".cfg") && !DirsToSkip.Contains(Path.GetDirectoryName(key)!) && !FilesToSkip.Contains(Path.GetFileName(key)));

        if (numberOfFilesToTest.HasValue)
        {
            configFiles = configFiles.Take(numberOfFilesToTest.Value);
        }

        var tasks = configFiles.Select(async key =>
        {
            var text = await _provider.LoadPakFile(key);
            return (key, text);
        });

        return (pak, (await Task.WhenAll(tasks)).ToList());
    }

    private static (bool allEquals, int filesWithLinesCountMismatch, int filesWithLinesContentMismatch) GetResults(VerificationResult[] results)
    {
        var filesTestedCount = results.Length;
        var allEquals = results.All(r => r.Success);
        var filesWithLinesCountMismatch = results.Count(r => r is { Success: false, LinesCountMismatch: true });
        var filesWithLinesContentMismatch = results.Count(r => r is { Success: false, MismatchedLines.Count: > 0 });
        return (allEquals, filesWithLinesCountMismatch, filesWithLinesContentMismatch);
    }
}

#endif