using System.Text;
using DiffPlex.DiffBuilder.Model;
using DiffPlex.DiffBuilder;
using DiffPlex;
using Stalker2PakCfgMergeTool.Entities;
using Stalker2PakCfgMergeTool.Interfaces;

namespace Stalker2PakCfgMergeTool;

public class PakMerger : IDisposable
{
    private const string DiffHtmlFileName = "diff.html";

    private readonly IPakProvider _pakProvider;
    private readonly IPakProvider _referencePakProvider;
    private readonly IFileMerger _fileMerger;

    public PakMerger(IPakProvider pakProvider, IPakProvider referencePakProvider, IFileMerger fileMerger)
    {
        _pakProvider = pakProvider;
        _referencePakProvider = referencePakProvider;
        _fileMerger = fileMerger;
    }

    public async Task<List<PakFileWithContent>> MergePaksWithConflicts()
    {
        var paks = _pakProvider.GetPaksInfo();
        var conflicts = Debug.IsDebug
            ? FindConflictsForDebug(paks)
            : FindConflicts(paks);

        return await MergePaksWithConflicts(conflicts);
    }

    private static List<FileConflict> FindConflicts(List<Pak> paks, bool skipConflictsCheck = false)
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

        return (skipConflictsCheck ? fileConflicts :  fileConflicts.Where(kv => kv.Value.ConflictWith.Count > 1)).Select(kv => kv.Value).ToList();
    }

    private async Task<List<PakFileWithContent>> MergePaksWithConflicts(List<FileConflict> conflicts)
    {
        var pakFiles = new List<PakFileWithContent>();
        var diffHtmlList = new List<(string fileName, string diffHtml)>();

        foreach (var conflict in conflicts)
        {
            try
            {
                var (pak, diffHtmlWithFileName) = await MergePakWithConflicts(conflict);

                pakFiles.Add(pak);
                diffHtmlList.Add(diffHtmlWithFileName);
            }
            catch (Exception e)
            {

                Console.WriteLine($"Error merging {conflict.FilePath}: {e.Message}\n");
            }
        }

        if (diffHtmlList.Count == 0)
        {
            return pakFiles;
        }


        if (File.Exists(DiffHtmlFileName))
        {
            File.Delete(DiffHtmlFileName);
        }

        var diffHtml = GenerateTabsHtml(diffHtmlList.Select(diffHtmlWithFileName => (diffHtmlWithFileName.fileName, diffHtmlWithFileName.diffHtml)).ToList());

        await File.WriteAllTextAsync(DiffHtmlFileName, diffHtml);


        return pakFiles;
    }

    public void Dispose()
    {
        _pakProvider.Dispose();
        _referencePakProvider.Dispose();
    }

    private async Task<(PakFileWithContent pak, (string fileName, string diffHtlm))> MergePakWithConflicts(FileConflict conflict)
    {
        // Sort conflict with by pak name to have consistent output
        conflict.ConflictWith = conflict.ConflictWith.OrderByDescending(fc => fc.PakName).ToList();

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

        var modifiedTexts = new List<(string pakName, string modifiedText)>();
        foreach (var pakFile in conflict.ConflictWith)
        {
            var modifiedText = (pakFile.PakName, Debug.IsDebug && Debug.FolderPaks.Count > 0
                ? await File.ReadAllTextAsync(Path.Combine(Debug.FolderPaks[pakFile.PakName], pakFile.FilePath))
                : await _pakProvider.LoadPakFile(pakFile.FilePath, pakFile.PakName));
            modifiedTexts.Add(modifiedText);
        }

        (originalText, var mergedText) = _fileMerger.Merge(originalText, conflict.FileName, modifiedTexts);

        Console.WriteLine("Merged\n");

        originalText = string.Join('\n', originalText.Split('\n').Select(line => line.Trim()).Where(line => !line.StartsWith("//")));

        var diffHtml = GenerateSideBySideDiffHtml(originalText, mergedText, conflict.FileName, conflict.ConflictWith.Select(cw => cw.PakName).ToList());

        return (new PakFileWithContent
        {
            PakName = "merged",
            FileName = Path.GetFileName(conflict.FilePath),
            FilePath = conflict.FilePath,
            Content = Encoding.UTF8.GetBytes(mergedText)
        }, (conflict.FileName, diffHtml));
    }

    private static string GenerateTabsHtml(List<(string fileName, string diffHtml)> diffHtmlList)
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ViewTemplates", "DiffTemplate.html");
        var templateContent = File.ReadAllText(templatePath);

        var tabsScript = new StringBuilder();
        foreach (var (fileName, diffHtml) in diffHtmlList)
        {
            tabsScript.AppendLine($"addTab('{fileName}', `{diffHtml}`);");
        }

        var finalHtml = templateContent.Replace("<script>", $"<script>{tabsScript}");
        return finalHtml;
    }

    public static string GenerateSideBySideDiffHtml(string oldText, string newText, string fileName, List<string> modNames)
    {
        var diffBuilder = new SideBySideDiffBuilder(new Differ());
        var diffModel = diffBuilder.BuildDiffModel(oldText, newText);

        var htmlBuilder = new StringBuilder();
        htmlBuilder.Append($"<h2>File: {fileName}</h2>");
        htmlBuilder.Append("<h3>Mods used in merge:</h3><ul>");
        foreach (var modName in modNames)
        {
            htmlBuilder.Append($"<li>{modName}</li>");
        }
        htmlBuilder.Append("</ul>");
        htmlBuilder.Append("<table>");
        htmlBuilder.Append("<tr><th>Line</th><th>Original</th><th>Line</th><th>Modified</th></tr>");

        var lineNumber = 1;
        var unchangedCount = 0;
        var inUnchangedBlock = false;
        var unchangedClass = "";

        foreach (var diffPiece in diffModel.OldText.Lines.Zip(diffModel.NewText.Lines, (oldLine, newLine) => new { oldLine, newLine }))
        {
            if (diffPiece.oldLine.Type == ChangeType.Unchanged && diffPiece.newLine.Type == ChangeType.Unchanged)
            {
                unchangedCount++;
                switch (unchangedCount)
                {
                    case 5:
                        unchangedClass = $"unchanged-{lineNumber}";
                        htmlBuilder.Append($"<tr><td colspan='4' style='text-align:center;' id='placeholder-{unchangedClass}' class='expand-collapse' onclick='toggleUnchanged(\"{unchangedClass}\")'>Some unchanged lines are hidden, click to expand</td></tr>");
                        inUnchangedBlock = true;
                        break;
                    case > 5:
                        htmlBuilder.Append($"<tr class='{unchangedClass} unchanged' style='display:none;'><td class='line-number'>{lineNumber}</td><td class='truncate'>{diffPiece.oldLine.Text}</td><td class='line-number'>{lineNumber}</td><td class='truncate'>{diffPiece.newLine.Text}</td></tr>");
                        break;
                    default:
                        htmlBuilder.Append($"<tr><td class='line-number'>{lineNumber}</td><td class='truncate'>{diffPiece.oldLine.Text}</td><td class='line-number'>{lineNumber}</td><td class='truncate'>{diffPiece.newLine.Text}</td></tr>");
                        break;
                }
            }
            else
            {
                if (inUnchangedBlock)
                {
                    inUnchangedBlock = false;
                    unchangedCount = 0;
                }

                htmlBuilder.Append("<tr>");
                htmlBuilder.Append($"<td class='line-number'>{lineNumber}</td><td style='background-color:{GetBackgroundColor(diffPiece.oldLine.Type)}'>{HighlightChanges(diffPiece.oldLine.Text, diffPiece.oldLine.Type)}</td>");
                htmlBuilder.Append($"<td class='line-number'>{lineNumber}</td><td style='background-color:{GetBackgroundColor(diffPiece.newLine.Type)}'>{HighlightChanges(diffPiece.newLine.Text, diffPiece.newLine.Type)}</td>");
                htmlBuilder.Append("</tr>");
            }
            lineNumber++;
        }

        htmlBuilder.Append("</table>");
        return htmlBuilder.ToString();
    }

    private static string GetBackgroundColor(ChangeType changeType)
    {
        return changeType switch
        {
            ChangeType.Inserted => "#d4fcbc",
            ChangeType.Deleted => "#fbb6c2",
            ChangeType.Modified => "#ffe4b5",
            _ => "white",
        };
    }

    private static string HighlightChanges(string text, ChangeType changeType)
    {
        if (changeType != ChangeType.Modified)
        {
            return text;
        }

        var diffBuilder = new InlineDiffBuilder(new Differ());
        var diffModel = diffBuilder.BuildDiffModel(text, text);
        var highlightedText = new StringBuilder();

        foreach (var line in diffModel.Lines)
        {
            if (line.Type == ChangeType.Modified)
            {
                highlightedText.Append($"<span style='background-color:#ffe4b5;'>{line.Text}</span>");
            }
            else
            {
                highlightedText.Append(line.Text);
            }
        }

        return highlightedText.ToString();

    }

    private static List<FileConflict> FindConflictsForDebug(List<Pak> paks)
    {
        if (Debug.Paks.Count == 0 && Debug.FolderPaks.Count == 0)
        {
            return FindConflicts(paks);
        }

        if (Debug.FolderPaks.Count > 0)
        {
            paks = [];
            foreach(var (_, pakDir) in Debug.FolderPaks)
            {
                var dirInfo = new DirectoryInfo(pakDir);
                var dirs = dirInfo.GetDirectories();

                var pakFiles = dirs.SelectMany(d => d.GetFiles("*.cfg", SearchOption.AllDirectories)).Select(f => Path.GetRelativePath(dirInfo.FullName, f.FullName).Replace("\\", "/")).ToList();
                paks.Add(new Pak
                {
                    Name = dirInfo.Name,
                    PakFileKeys = pakFiles
                });
            }
        }
        else
        {
            paks = paks.Where(p => Debug.Paks.Contains(p.Name)).ToList();
        }

        return FindConflicts(paks, Debug.MergeWithoutConflict);
    }
}