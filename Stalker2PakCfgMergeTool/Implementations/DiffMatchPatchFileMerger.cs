using DiffMatchPatch;
using Stalker2PakCfgMergeTool.Interfaces;

namespace Stalker2PakCfgMergeTool.Implementations;

//public class DiffMatchPatchFileMerger : IFileMerger
//{
//    public string Merge(string originalText, List<string> modifiedTexts)
//    {
//        var dmp = new diff_match_patch();
//        var patches = new List<Patch>();
//        var appliedPatches = new HashSet<string>();

//        // TODO: Fix an issue where same lines can be inserted multiple times
//        foreach (var modifiedText in modifiedTexts)
//        {
//            var diffs = dmp.diff_main(originalText, modifiedText);
//            var newPatches = dmp.patch_make(originalText, diffs);

//            foreach (var patch in newPatches)
//            {
//                var patchText = dmp.patch_toText([patch]);

//                // this should prevent applying the same patch multiple times and not break anything... in theory
//                if (appliedPatches.Contains(patchText))
//                {
//                    continue;
//                }

//                patches.Add(patch);
//                appliedPatches.Add(patchText);
//            }
//        }

//        var patchesResult = dmp.patch_apply(patches, originalText);
//        var mergedText = (string)patchesResult[0];

//        return mergedText;
//    }
//}