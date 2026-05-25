using HarmonyLib;

namespace RageNAdrenaline.Patches;

[HarmonyPatch]
public class PlayerPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.Start))]
    private static void AwakePostFix(Player __instance)
    {
        if (__instance == null) return;
        if (__instance.m_nview == null || !__instance.m_nview.IsValid()) return;
        
        __instance.m_nview.Register("RPC_ResetAdrenalineMeter", (sender) =>
        {
            if (!__instance.IsOwner() || Plugin.AdrenalineMeter == null)
            {
                Plugin.Logger.LogWarning("RPC_ResetAdrenalineMeter was called by a non-owner or the Adrenaline meter is null");
                return;
            }
            Plugin.Logger.LogInfo("Resetting Adrenaline meter for " + __instance.name);
            Plugin.AdrenalineMeter.ResetValue();
                
            __instance.m_nview.GetZDO().Set("RageNAdrenaline_AdrenalineFull", false);
            Plugin.Logger.LogInfo("Adrenaline meter reset for " + __instance.name);
        });
    }
}