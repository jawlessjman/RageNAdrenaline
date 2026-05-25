using HarmonyLib;
using RageNAdrenaline.Data;
using UnityEngine;

namespace RageNAdrenaline.Patches;

/// <summary>
/// Patches for the character
/// </summary>
[HarmonyPatch]
public class CharacterPatch
{
    /// <summary>
    /// Remove Adrenaline when taking damage
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="hit"></param>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    public static void DamagePreFix(Character __instance, ref HitData hit)
    {
        if (__instance == null) return;
        if (!__instance.IsPlayer()) return;
        if (hit.GetTotalDamage() <= 0) return;

        if (__instance.m_nview == null || !__instance.m_nview.IsValid()) return;
        var isAdrenalineActive = __instance.m_nview.GetZDO().GetBool("RageNAdrenaline_AdrenalineFull", false);

        if (!isAdrenalineActive) return;
        // Apply reduction safely on whatever machine runs this damage calculation
        hit.m_damage.m_damage *= Plugin.AdrenalineDamageReduction.Value;

        // Play sound locally for everyone nearby
        var sound = AssetHolder.GetAudioClip("AdrenalineMajorLoss");
        if (sound != null)
        {
            AudioSource.PlayClipAtPoint(sound, __instance.transform.position);
        }
        
        __instance.m_nview.InvokeRPC("RPC_ResetAdrenalineMeter");
    }
}