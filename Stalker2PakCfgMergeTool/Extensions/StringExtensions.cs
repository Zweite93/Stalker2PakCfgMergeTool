namespace Stalker2PakCfgMergeTool.Extensions;

public static class StringExtensions
{
    public static string RemoveComments(this string line)
    {
        var commentIndex = line.IndexOf("//", StringComparison.InvariantCulture);
        return commentIndex >= 0 ? line[..commentIndex].TrimEnd() : line;
    }

    public static string RemoveBom(this string text)
    {
        return text.Length > 0 && text[0] == '\uFEFF' ? text[1..] : text;
    }
}