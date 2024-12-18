﻿using Stalker2PakCfgMergeTool.Interfaces;
using System.Text.RegularExpressions;
using Stalker2PakCfgMergeTool.Entities;

namespace Stalker2PakCfgMergeTool.Implementations;

public partial class ConfigSerializerVerifier : IConfigSerializerVerifier
{
    private readonly IConfigSerializer _configSerializer;

    public ConfigSerializerVerifier(IConfigSerializer configSerializer)
    {
        _configSerializer = configSerializer;
    }

    public VerificationResult Verify(Config config, string configText)
    {
        var verificationResult = new VerificationResult { Success = true };

        configText = NormalizeText(configText);
        var serializedText = _configSerializer.Serialize(config);

        if (string.Equals(configText.Trim(), serializedText.Trim()))
        {
            return verificationResult;
        }

        var configLines = configText.Trim().Split("\n").ToArray();
        var serializedLines = serializedText.Trim().Split("\n").ToArray();

        if (configLines.Length != serializedLines.Length)
        {
            verificationResult.Success = false;
            verificationResult.LinesCountMismatch = true;
            return verificationResult;
        }

        for (var i = 0; i < configLines.Length; i++)
        {
            var line = NormalizeLine(configLines[i]);
            var resultLine = NormalizeLine(serializedLines[i]);

            if (resultLine != line)
            {
                verificationResult.Success = false;
                verificationResult.MismatchedLines.Add($"Line {i + 1}:\nExpected: {line}\nActual: {resultLine}");
            }
        }

        return verificationResult;
    }

    private static string NormalizeText(string text)
    {
        var lines = text.Split("\n").ToArray();
        lines = lines.Where(l => !l.TrimStart().StartsWith("//") && !string.IsNullOrWhiteSpace(l)).Select(l => l.Replace("\t", "   ")).ToArray();
        return string.Join("\n", lines);
    }

    private static string NormalizeLine(string input)
    {
        input = input.Trim().Replace("  ", " ");
        input = NormalizeSeparatorsRegex().Replace(input, " $1 ");

        // Normalize whitespace around '{' and '}'
        input = NormalizeCurlyBracesRegex().Replace(input, " { ");
        input = NormalizeCurlyBracesCloseRegex().Replace(input, " } ");

        return input;
    }

    [GeneratedRegex(@"\s*([:=])\s*")]
    private static partial Regex NormalizeSeparatorsRegex();

    [GeneratedRegex(@"\s*{\s*")]
    private static partial Regex NormalizeCurlyBracesRegex();

    [GeneratedRegex(@"\s*}\s*")]
    private static partial Regex NormalizeCurlyBracesCloseRegex();
}

public class VerificationResult
{
    public bool Success { get; set; }
    public bool LinesCountMismatch { get; set; }
    public List<string> MismatchedLines { get; set; } = [];
}