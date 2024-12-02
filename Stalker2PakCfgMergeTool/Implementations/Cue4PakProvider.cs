using System.Text;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Stalker2PakCfgMergeTool.Entities;
using Stalker2PakCfgMergeTool.Enums;
using Stalker2PakCfgMergeTool.Interfaces;

namespace Stalker2PakCfgMergeTool.Implementations;

public class Cue4PakProvider : IPakProvider
{
    private const string OriginalPakPrefix = "pakchunk0-";

    private readonly DefaultFileProvider _originalPaksProvider;
    private readonly DefaultFileProvider _modPaksProvider;

    public Cue4PakProvider(string originalPaksDir, string modPaksDir, string aesKey)
    {
        _originalPaksProvider = new DefaultFileProvider(originalPaksDir, SearchOption.TopDirectoryOnly, false, new VersionContainer(EGame.GAME_UE5_1));
        _originalPaksProvider.Initialize();
        _originalPaksProvider.SubmitKey(new FGuid(), new FAesKey(aesKey));

        _modPaksProvider = new DefaultFileProvider(modPaksDir, SearchOption.AllDirectories, false, new VersionContainer(EGame.GAME_UE5_1));
        _modPaksProvider.Initialize();
        _modPaksProvider.SubmitKey(new FGuid(), new FAesKey(aesKey));
    }

    public List<Pak> GetPaksInfo(PakSearchOption pakSearchOption)
    {
        var pakProvider = pakSearchOption == PakSearchOption.OriginalPaks ? _originalPaksProvider : _modPaksProvider;

        var paks = new List<Pak>();
        foreach (var mountedVfs in pakProvider.MountedVfs)
        {
            if (mountedVfs.Name.Contains(Constants.MergedPakBaseName))
            {
                continue;
            }

            if (pakSearchOption == PakSearchOption.OriginalPaks && (!mountedVfs.Name.StartsWith(OriginalPakPrefix) || !mountedVfs.Name.EndsWith(".pak")))
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

            if (pakSearchOption == PakSearchOption.OriginalPaks)
            {
                break;
            }
        }

        return paks;
    }

    public async Task<string> LoadPakFile(string pakFilePath, PakSearchOption pakSearchOption, string? pakName = null)
    {

        if (pakSearchOption == PakSearchOption.OriginalPaks)
        {
            var files = _originalPaksProvider.MountedVfs.FirstOrDefault(mv => mv.Name.StartsWith(OriginalPakPrefix) && mv.Name.EndsWith(".pak"))?.Files ?? new Dictionary<string, GameFile>();

            if (files.Count == 0)
            {
                throw new Exception("No original paks found.");
            }

            var text = await LoadPakFile(pakFilePath, files);

            if (text != null)
            {
                return text;
            }

            // original cfg file is most likely in pakchunk0-Windows.pak or pakchunk0-WinGDK.pak, but if it's not, find it in any mounted pak
            var otherOriginalPakFiles = _originalPaksProvider.MountedVfs.Where(mv => !mv.Name.StartsWith(OriginalPakPrefix) && mv.Name.EndsWith(".pak")).SelectMany(mv => mv.Files).ToDictionary();
            text = await LoadPakFile(pakFilePath, otherOriginalPakFiles);

            if (text != null)
            {
                return text;
            }

            throw new Exception($"File '{pakFilePath}' not found in original paks.");
        }
        else
        {
            if (string.IsNullOrEmpty(pakName))
            {
                throw new Exception("Pak name must be provided when searching in mod paks.");
            }

            var files = _modPaksProvider.MountedVfs.FirstOrDefault(mv => mv.Name == pakName)?.Files ?? new Dictionary<string, GameFile>();

            if (files.Count == 0)
            {
                throw new Exception($"No mod paks found with name '{pakName}'.");
            }

            var text = await LoadPakFile(pakFilePath, files);

            if (text != null)
            {
                return text;
            }

            throw new Exception($"File '{pakFilePath}' not found in mod paks with name '{pakName}'.");
        }
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

    public void Dispose()
    {
        _modPaksProvider.UnloadAllVfs();
        _modPaksProvider.Dispose();
    }

    private static async Task<string?> LoadPakFile(string pakFilePath, IReadOnlyDictionary<string, GameFile> files)
    {
        if (files.Count < 0 || !files.TryGetValue(pakFilePath, out var gameFile))
        {
            return null;
        }

        var bytes = await gameFile.ReadAsync();

        var text = Encoding.UTF8.GetString(bytes);

        // Remove BOM if present
        if (text.Length > 0 && text[0] == '\uFEFF')
        {
            return text[1..];
        }

        return text;
    }
}