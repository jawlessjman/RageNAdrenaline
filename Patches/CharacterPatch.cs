using HarmonyLib;
using RageNAdrenaline.Data;
using UnityEngine;

namespace RageNAdrenaline.Patches;

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
        if (__instance.IsBlocking())
        {
            Plugin.Logger.LogInfo("Blocking");
            return;
        }

        if (Plugin.AdrenalineMeter.HasMaxPower()) // Remove half of the damage if the player has max adrenaline
        {
            hit.m_damage.m_damage *= 0.5f;
        }

        if (Plugin.AdrenalineMeter.GetValue() > 0)
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
    /// Patch to prevent default adrenaline bar usage
    /// </summary>
    /// <param name="__instance"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), nameof(Character.AddAdrenaline))]
    public static bool AddAdrenalinePreFix(Character __instance)
    {
        Plugin.Logger.LogInfo("AddAdrenalinePreFix");
        return false;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
    public static void OnDeathPostFix(Character __instance)
    {
        if (__instance == null)
        {
            Plugin.Logger.LogInfo("Character is null");
            return;
        }
        
        if (__instance == Player.m_localPlayer)
        {
            Plugin.AdrenalineMeter.ResetValue();
            Plugin.AdrenalineMeter.SetShouldRegen(false);
            
            Plugin.RageMeter.ResetValue();
            Plugin.RageMeter.SetShouldRegen(false);
            
            return;
        }
        
        if (!__instance.IsBoss()) return;
        
        Plugin.AdrenalineMeter.ResetValue();   
        Plugin.AdrenalineMeter.SetShouldRegen(false);
    }
}