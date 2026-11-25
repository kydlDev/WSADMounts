using System.Collections.Generic;
using KydlCore.Common;
using UnityEngine;
using WSADMounts.Bootstrap;
using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Features.Riding.Infrastructure;

/// <summary>
/// The hand-made backward walk clips (D-032) from the mod's AssetBundle, by mount prefab ("WSADMounts_Lox_WalkBack"). Looked up once per mount
/// type and cached, a missing clip included (that mount then shows the reversed walk, D-035).
/// </summary>
internal static class HandMadeClips
{
    private static readonly Dictionary<string, AnimationClip> Cache = new Dictionary<string, AnimationClip>();

    /// <summary>The hand-made clip for this mount. Expected outcome: the clip, or null (other mount, or not in the bundle yet).</summary>
    internal static AnimationClip For(string prefabName) => Named(BackwardWalkRules.HandMadeClipName(prefabName), prefabName);

    /// <summary>The hand-made jump clip for this mount (D-040). Expected outcome: the clip, or null (other mount, or not in the bundle).</summary>
    internal static AnimationClip JumpFor(string prefabName) => Named(JumpClipRules.ClipName(prefabName), prefabName);

    /// <summary>A clip of the bundle by asset name ("" = none), cached. Expected outcome: the clip, or null.</summary>
    private static AnimationClip Named(string asset, string prefabName)
    {
        if (asset.Length > 0 && !Cache.ContainsKey(asset))
        {
            AnimationClip loaded = Plugin.Bundle != null ? Plugin.Bundle.LoadAsset<AnimationClip>(asset) : null;
            Cache[asset] = loaded;
            ModLog.State($"hand-made clip {asset}", "unknown", loaded != null ? "loaded" : "missing", loaded != null ? $"{loaded.length:0.##} s" : (Plugin.Bundle == null ? "the mod has no bundle" : "not in the bundle"));
        }

        AnimationClip result = asset.Length > 0 ? Cache[asset] : null;
        ModLog.Every("HandMadeClips.Named", 5d, $"HandMadeClips.Named({prefabName}, '{asset}') -> {(result != null ? result.name : "none")}");
        return result;
    }
}
