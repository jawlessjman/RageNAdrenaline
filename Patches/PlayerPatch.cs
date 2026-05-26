using HarmonyLib;
using RageNAdrenaline.Data;
using UnityEngine;

namespace RageNAdrenaline.Patches;

/// <summary>
/// Patches the player
/// </summary>
[HarmonyPatch]
public static class PlayerPatch
{
    /// <summary>
    /// Patches the player's Awake method'
    /// </summary>
    /// <param name="__instance">A player</param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    private static void AwakePostfix(Player __instance)
    {
        if (__instance == null) return;
        if (__instance != Player.m_localPlayer) return;
        
        Plugin.Logger.LogInfo("PlayerPatch: AwakePostfix");
        
        //__instance.m_onDamaged += OnPlayerDamaged;
        __instance.m_onDeath += OnPlayerDeath;
    }

    /// <summary>
    /// Patches the player's Damage method'
    /// </summary>
    /// <param name="__instance">A character</param>
    /// <param name="hit">The hit data</param>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    private static void OnDamagedPrefix(Character __instance, HitData hit)
    {
        if (__instance == null) return;
        if (!__instance.IsPlayer()) return;
        if (Player.m_localPlayer == null) return;
        if (__instance != Player.m_localPlayer) return;
        
        var hasMaxPower = Plugin.AdrenalineMeter.HasMaxPower();
        
        Plugin.AdrenalineMeter.ResetValue();
        
        if (!hasMaxPower)
        {
            Plugin.Logger.LogInfo("PlayerPatch: OnDamagedPrefix: Adrenaline is not maxed");
            return;
        }
        
        var sound = AssetHolder.GetAudioClip("AdrenalineMajorLoss");

        if (sound != null)
        {
            AudioSource.PlayClipAtPoint(
                sound,
                Player.m_localPlayer.transform.position
            );
        }
        
        Plugin.Logger.LogInfo($"PlayerPatch: OnDamagedPrefix - damage pre: {hit.GetTotalDamage()}");
        hit.ApplyModifier(Plugin.AdrenalineDamageReduction.Value);
        Plugin.Logger.LogInfo($"PlayerPatch: OnDamagedPrefix - damage post: {hit.GetTotalDamage()}");
    }

    /// <summary>
    /// Patches the player's Damage method'
    /// </summary>
    /// <param name="damage">The amount of damage dealt</param>
    /// <param name="hit">The character that was hit</param>
    private static void OnPlayerDamaged(float damage, Character hit)
    {
        if (Plugin.AdrenalineMeter.HasMaxPower())
        {
            var sound = AssetHolder.GetAudioClip("AdrenalineMajorLoss");

            if (sound != null)
            {
                AudioSource.PlayClipAtPoint(
                    sound,
                    Player.m_localPlayer.transform.position
                );
            }
        }
        
        Plugin.AdrenalineMeter.ResetValue();
    }

    /// <summary>
    /// Patches the player's Death method
    /// </summary>
    private static void OnPlayerDeath()
    {
        Plugin.AdrenalineMeter.ResetValue();
        Plugin.RageMeter.ResetValue();
    }
}