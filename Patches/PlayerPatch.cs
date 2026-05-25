using HarmonyLib;
using RageNAdrenaline.Data;
using UnityEngine;

namespace RageNAdrenaline.Patches;

[HarmonyPatch]
public static class PlayerPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    private static void AwakePostfix(Player __instance)
    {
        if (__instance == null)
        {
            Plugin.Logger.LogError("PlayerPatch: AwakePostfix: __instance is null");
            return;
        }

        if (__instance != Player.m_localPlayer)
        {
            Plugin.Logger.LogInfo("PlayerPatch: AwakePostfix: Not local player");
            return;
        }
        
        Plugin.Logger.LogInfo("PlayerPatch: AwakePostfix");
        
        __instance.m_onDamaged += OnPlayerDamaged;
        __instance.m_onDeath += OnPlayerDeath;
    }

    private static void OnPlayerDamaged(float damage, Character hit)
    {
        Plugin.Logger.LogInfo($"PlayerPatch: OnPlayerDamaged: {damage}");

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

    private static void OnPlayerDeath()
    {
        Plugin.Logger.LogInfo("PlayerPatch: OnPlayerDeath");
        Plugin.AdrenalineMeter.ResetValue();
        
        Plugin.RageMeter.ResetValue();
    }
}