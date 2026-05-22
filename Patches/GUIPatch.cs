using System;
using HarmonyLib;
using RageNAdrenaline.Data;
using RageNAdrenaline.Data.Enums;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RageNAdrenaline.Patches;

[HarmonyPatch]
public class GUIPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
    private static void CreateCustomBars(Hud __instance)
    {
        var eitrPanel = __instance.m_rootObject.transform.Find("eitrpanel");

        if (eitrPanel == null)
        {
            LogString.LogMessage("EITR panel not found");
            return;
        }

        var vanillaBar = eitrPanel.Find("Stamina");

        if (vanillaBar == null)
        {
            LogString.LogMessage("Vanilla bar not found");
            return;
        }
        
        Plugin.Logger.LogInfo("Bar found");
        
        var targetParent = __instance.m_rootObject.transform;
        var targetPosition = Vector2.zero;

        switch (Plugin.BarLocation.Value)
        {
            case BarLocation.Hotbar:
                targetParent = __instance.m_rootObject.transform.Find("HotKeyBar");
                //targetPosition = new Vector2(-150f, -160f);
                break;
            case BarLocation.Normal:
                targetParent = __instance.m_rootObject.transform;
                //targetPosition = new Vector2(450f, -20f);
                break;
            case BarLocation.Minimap:
                targetParent = __instance.m_rootObject.transform.Find("MiniMap");
                //targetPosition = new Vector2(0f, -180f);
                break;
        }
        
        var instance = Object.Instantiate(vanillaBar.gameObject, targetParent);
        instance.name = "AdrenalineBar";
        
        var barComponent = instance.GetComponent<GuiBar>();
        if (barComponent == null)
        {
            Plugin.Logger.LogError("Bar component not found");
            barComponent = instance.GetComponentInChildren<GuiBar>();

            if (barComponent == null)
            {
                Plugin.Logger.LogError("Bar component not found in children");
                return;
            }
            
            LogString.LogMessage("Bar component found");
        }
        
        barComponent.SetMaxValue(Adrenaline.GetMaxAdrenalineLevel());
        barComponent.SetValue(Adrenaline.GetAdrenalineLevel() + 5);
        
        var rect = instance.GetComponent<RectTransform>();
        if (rect == null)
        {
            Plugin.Logger.LogError("Rect transform not found");
            return;
        }
        
        rect.anchoredPosition = targetPosition;
        
        var barImages = instance.GetComponentsInChildren<Image>();
        
        if (barImages == null)
        {
            Plugin.Logger.LogError("Bar image not found");
            return;
        }

        foreach (var img in barImages)
        {
            LogString.LogMessage($"Image name: {img.name}");
            if (img.gameObject.name.ToLower() == "darken")
            {
                img.gameObject.SetActive(false);
                continue;
            }
            
            if (img.gameObject.name.ToLower().Contains("bar") || img.gameObject.name.ToLower().Contains("fill"))
            {
                img.color = (new Color(0.9f, 0.2f, 0.1f, 1f));
            }
        }
        
        var textsToHide = instance.GetComponentsInChildren<Text>();
        foreach (var text in textsToHide)
        {
            text.gameObject.SetActive(false);
        }
        
        Plugin.Logger.LogInfo("Success");
    }
}