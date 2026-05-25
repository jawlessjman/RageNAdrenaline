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
            if (!__instance.IsOwner() || Plugin.AdrenalineMeter == null) return;
            Plugin.AdrenalineMeter.ResetValue();
                
            __instance.m_nview.GetZDO().Set("RageNAdrenaline_AdrenalineFull", false);
        });
    }
}