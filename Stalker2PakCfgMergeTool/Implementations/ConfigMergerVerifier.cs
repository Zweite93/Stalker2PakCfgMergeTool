using Stalker2PakCfgMergeTool.Interfaces;

namespace Stalker2PakCfgMergeTool.Implementations;

public class ConfigMergerVerifier : IConfigMergerVerifier
{
    private readonly IConfigSerializer _configSerializer;
    private readonly IFileMerger _fileMerger;

    public ConfigMergerVerifier(IConfigSerializer configSerializer, IFileMerger fileMerger)
    {
        _configSerializer = configSerializer;
        _fileMerger = fileMerger;
    }

    public VerificationResult Verify(string fileName, string pakName, string configText)
    {
        var verificationResult = new VerificationResult { Success = true };

        var config = _configSerializer.Deserialize(fileName, pakName, configText);

        // merging config with itself should produce identical config as input
        var mergedText = _fileMerger.Merge(configText, fileName, [(pakName, configText), (pakName, configText)]).mergedText;

        var referenceText = _configSerializer.Serialize(config);

        if (string.Equals(referenceText.Trim(), mergedText.Trim()))
        {
            return verificationResult;
        }

        var referenceLines = referenceText.Trim().Split("\n").ToArray();
        var mergedLines = mergedText.Trim().Split("\n").ToArray();

        if (referenceLines.Length != mergedLines.Length)
        {
            verificationResult.Success = false;
            verificationResult.LinesCountMismatch = true;
            return verificationResult;
        }

        for (var i = 0; i < referenceLines.Length; i++)
        {
            var referenceLine = referenceLines[i];
            var mergedLine = mergedLines[i];

            if (mergedLine != referenceLine)
            {
                verificationResult.Success = false;
                verificationResult.MismatchedLines.Add($"Line {i + 1}:\nExpected: {referenceLine}\nActual: {mergedLine}");
            }
        }

        return verificationResult;
    }
}