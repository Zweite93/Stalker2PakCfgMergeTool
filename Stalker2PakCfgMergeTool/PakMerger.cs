using System.Text;
using System.Text.RegularExpressions;
using DiffMatchPatch;
using Stalker2PakCfgMergeTool.Entities;
using Stalker2PakCfgMergeTool.Interfaces;

namespace Stalker2PakCfgMergeTool;

public class PakMerger : IDisposable
{
    private const string DiffHtmlFileName = "diff.html";

    private readonly IPakProvider _pakProvider;
    private readonly IPakProvider _referencePakProvider;

    public PakMerger(IPakProvider pakProvider, IPakProvider referencePakProvider)
    {
        _pakProvider = pakProvider;
        _referencePakProvider = referencePakProvider;
    }

    public async Task<List<PakFileWithContent>> MergePaksWithConflicts()
    {
        var paks = _pakProvider.GetPaksInfo();
        var conflicts = FindConflicts(paks);

        return await MergePaksWithConflicts(conflicts);
    }

    private static List<FileConflict> FindConflicts(List<Pak> paks)
    {
        var fileConflicts = new Dictionary<string, FileConflict>();
        foreach (var pak in paks)
        {
            foreach (var pakFile in pak.PakFileKeys)
            {
                if (fileConflicts.ContainsKey(pakFile))
                {
                    fileConflicts[pakFile].ConflictWith.Add(new PakFile
                    {
                        PakName = pak.Name,
                        FileName = Path.GetFileName(pakFile),
                        FilePath = pakFile
                    });
                }
                else
                {
                    fileConflicts.Add(pakFile, new FileConflict
                    {
                        FileName = Path.GetFileName(pakFile),
                        FilePath = pakFile,
                        ConflictWith =
                        [
                            new PakFile
                            {
                                PakName = pak.Name,
                                FileName = Path.GetFileName(pakFile),
                                FilePath = pakFile
                            }
                        ]
                    });
                }
            }
        }

        return fileConflicts.Where(kv => kv.Value.ConflictWith.Count > 1).Select(kv => kv.Value).ToList();
    }

    private async Task<List<PakFileWithContent>> MergePaksWithConflicts(List<FileConflict> conflicts)
    {
        var pakFiles = new List<PakFileWithContent>();
        var diffHtmlList = new List<string>();

        foreach (var conflict in conflicts.OrderBy(c => c.FileName))
        {
            try
            {
                var (pak, diffHtml) = await MergePakWithConflicts(conflict);

                pakFiles.Add(pak);
                diffHtmlList.Add(diffHtml);
            }
            catch (Exception e)
            {

                Console.WriteLine($"Error merging {conflict.FilePath}: {e.Message}\n");
            }
        }

        if (diffHtmlList.Count > 0)
        {
            var diffHtml = $"Differences from original game file:<br><br>{string.Join("\n", diffHtmlList)}";

            if (File.Exists(DiffHtmlFileName))
            {
                File.Delete(DiffHtmlFileName);
            }

            await File.WriteAllTextAsync(DiffHtmlFileName, diffHtml);
        }

        return pakFiles;
    }

    public void Dispose()
    {
        _pakProvider.Dispose();
        _referencePakProvider.Dispose();
    }

    private async Task<(PakFileWithContent pak, string diffHtlm)> MergePakWithConflicts(FileConflict conflict)
    {
        // Sort conflict with by pak name to have consistent output
        conflict.ConflictWith = conflict.ConflictWith.OrderBy(fc => fc.PakName).ToList();

        Console.WriteLine($"Merging {conflict.FilePath}\n");
        Console.WriteLine("Conflict with:");
        foreach (var modFile in conflict.ConflictWith)
        {
            Console.WriteLine($" - {modFile.PakName}");
        }

        Console.WriteLine();

        string originalText;
        try
        {
            originalText = await _referencePakProvider.LoadPakFile(conflict.FilePath);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error loading original file {conflict.FilePath}: {e.Message}.\nUsing first modified file as original instead.\n");
            originalText = await _pakProvider.LoadPakFile(conflict.FilePath, conflict.ConflictWith[0].PakName);
        }

        var dmp = new diff_match_patch();
        var patches = new List<Patch>();
        var appliedPatches = new HashSet<string>();

        // TODO: Fix an issue where same lines can be inserted multiple times
        foreach (var pakFile in conflict.ConflictWith)
        {
            var modifiedText = await _pakProvider.LoadPakFile(pakFile.FilePath, pakFile.PakName);
            var diffs = dmp.diff_main(originalText, modifiedText);
            var newPatches = dmp.patch_make(originalText, diffs);

            foreach (var patch in newPatches)
            {
                var patchText = dmp.patch_toText([patch]);

                // this should prevent applying the same patch multiple times and not break anything... in theory
                if (appliedPatches.Contains(patchText))
                {
                    continue;
                }

                patches.Add(patch);
                appliedPatches.Add(patchText);
            }
        }

        var patchesResult = dmp.patch_apply(patches, originalText);
        var textResult = (string)patchesResult[0];

        Console.WriteLine("Merged\n");

        var diffsSummary = dmp.diff_main(originalText, textResult);
        dmp.diff_cleanupSemantic(diffsSummary);

        var diffHtml = dmp.diff_prettyHtml(diffsSummary);
        var diffHtmlChangesOnly = string.Join(string.Empty, GetChangedLinesFromDiffHtml(diffHtml));
        diffHtml = $"<br>- File: {conflict.FileName}{(diffHtmlChangesOnly.StartsWith("<br>") ? string.Empty : "<br>")}<br>{diffHtmlChangesOnly}<br><br>";

        return (new PakFileWithContent
        {
            PakName = "merged",
            FileName = Path.GetFileName(conflict.FilePath),
            FilePath = conflict.FilePath,
            Content = Encoding.UTF8.GetBytes(textResult)
        }, diffHtml);
    }

    private static List<string> GetChangedLinesFromDiffHtml(string diffHtml)
    {
        var changedLines = new List<string>();

        // Split the diffHtml into lines
        var lines = diffHtml.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

        // Regular expressions to match ins, del, and span tags with diffmod class
        var insRegex = new Regex(@"<ins[^>]*>.*?<\/ins>", RegexOptions.Singleline);
        var delRegex = new Regex(@"<del[^>]*>.*?<\/del>", RegexOptions.Singleline);
        var modRegex = new Regex(@"<span[^>]*class=""diffmod""[^>]*>.*?<\/span>", RegexOptions.Singleline);

        // Search for changes in each line
        foreach (var line in lines)
        {
            if (insRegex.IsMatch(line) || delRegex.IsMatch(line) || modRegex.IsMatch(line))
            {
                var cleanedLine = line.Replace("&para;", string.Empty);
                changedLines.Add(cleanedLine);
            }
        }

        return changedLines;
    }
}