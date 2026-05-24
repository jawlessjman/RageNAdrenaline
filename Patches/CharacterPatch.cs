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
        if (__instance == null)
        {
            Plugin.Logger.LogInfo("Character is null");
            return;
        }
        if (__instance != Player.m_localPlayer) return;
        if (hit.GetTotalDamage() <= 0)
        {
            Plugin.Logger.LogInfo("No damage or negative damage");
            return;
        }

        if (Plugin.AdrenalineMeter.HasMaxPower()) // If the adrenaline meter is max, then reduce the damage taken
        {
            hit.m_damage.m_damage *= Plugin.AdrenalineDamageReduction.Value;
        }

        if (Plugin.AdrenalineMeter.GetValue() > 0) // Play sound when getting hit
        {
            var sound = AssetHolder.GetAudioClip("AdrenalineMajorLoss");
            if (sound != null)
            {
                AudioSource.PlayClipAtPoint(sound, __instance.transform.position);
            }
        }
        
        Plugin.AdrenalineMeter.ResetValue(); // Remove all adrenaline when taking damage
    }
    
    /// <summary>
    /// Post fix for when the player or boss dies
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
    public static void OnDeathPostFix(Character __instance)
    {
        if (__instance == null)
        {
            Plugin.Logger.LogInfo("Character is null");
            return;
        }
        
        // If player dies reset adrenaline and rage
        if (__instance == Player.m_localPlayer)
        {
            Plugin.AdrenalineMeter.ResetValue();
            Plugin.AdrenalineMeter.SetShouldRegen(false);
            
            Plugin.RageMeter.ResetValue();
            Plugin.RageMeter.SetShouldRegen(false);
            
            return;
        }
        
        if (!__instance.IsBoss()) return;
        // If it's a boss reset adrenaline
        Plugin.AdrenalineMeter.ResetValue();   
        Plugin.AdrenalineMeter.SetShouldRegen(false);
    }
}