using System.Text;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Stalker2PakCfgMergeTool.Entities;
using Stalker2PakCfgMergeTool.Interfaces;

namespace Stalker2PakCfgMergeTool.Implementations;

public class Cue4PakProvider : IPakProvider
{
    private readonly DefaultFileProvider _provider;
    private readonly string? _pakName;

    public Cue4PakProvider(string pakDir, string aesKey, string? pakName = null)
    {
        _pakName = pakName;
        _provider = new DefaultFileProvider(pakDir, SearchOption.AllDirectories, false, new VersionContainer(EGame.GAME_UE5_1));
        _provider.Initialize();
        _provider.SubmitKey(new FGuid(), new FAesKey(aesKey));
    }

    public List<Pak> GetPaksInfo()
    {
        var paks = new List<Pak>();
        foreach (var mountedVfs in _provider.MountedVfs)
        {
            if (mountedVfs.Name.Contains(Constants.MergedPakBaseName))
            {
                continue;
            }

            if (_pakName != null && mountedVfs.Name != _pakName)
            {
                continue;
            }

            var pak = new Pak
            {
                Name = mountedVfs.Name,
                PakFileKeys = []
            };

            foreach (var filePath in mountedVfs.Files.Keys)
            {
                if (Path.GetExtension(filePath) != ".cfg")
                {
                    continue;
                }

                pak.PakFileKeys.Add(filePath);
            }

            paks.Add(pak);

            if (_pakName != null)
            {
                break;
            }
        }

        return paks;
    }

    public async Task<string> LoadPakFile(string pakFilePath, string? pakName)
    {
        byte[] bytes;
        if (pakName == null)
        {
            bytes = await ReadFile(pakFilePath, _provider.Files, _pakName);
        }
        else
        {
            bytes = await ReadFile(pakFilePath, _provider.MountedVfs.First(mv => mv.Name == pakName).Files, pakName);
        }

        return Encoding.UTF8.GetString(bytes);
    }

    public void Dispose()
    {
        _provider.UnloadAllVfs();
        _provider.Dispose();
    }

    private async Task<byte[]> ReadFile(string pakFilePat, IReadOnlyDictionary<string, GameFile> files, string? pakName)
    {
        if (files.TryGetValue(pakFilePat, out var gameFile))
        {
            return await gameFile.ReadAsync();
        }

        // if not found in mod paks, throw exception, something is wrong
        if (pakName == null)
        {
            throw new FileNotFoundException($"File {pakFilePat} not found in {pakName}");
        }

        // original cfg file is most likely in pakchunk0-Windows.pak, but if it's not, find it in any mounted pak
        _provider.Files.TryGetValue(pakFilePat, out gameFile);

        if (gameFile == null)
        {
            throw new FileNotFoundException($"File {pakFilePat} not found in any mounted pak");
        }

        return await gameFile.ReadAsync();
    }

    public static async Task<bool> TestAesKey(string aesKey, string pakDir)
    {
        var testKeyIsValid = false;

        using var provider = new DefaultFileProvider(pakDir, SearchOption.TopDirectoryOnly, false, new VersionContainer(EGame.GAME_UE5_1));
        provider.Initialize();

        await provider.SubmitKeyAsync(new FGuid(), new FAesKey(aesKey));
        testKeyIsValid = provider.MountedVfs.Count > 0;

        return testKeyIsValid;
    }
}