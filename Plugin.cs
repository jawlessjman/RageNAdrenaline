using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Jotunn.Managers;
using RageNAdrenaline.Data;
using RageNAdrenaline.Data.Enums;
using UnityEngine;

namespace RageNAdrenaline;

[BepInPlugin(ModGuid, ModName, ModVersion)]
[BepInDependency(Jotunn.Main.ModGuid)]
public class Plugin : BaseUnityPlugin
{
    internal new static ManualLogSource Logger;

    // Position anchorMin & anchorMax      | Ideal pivot           | Explanation
    // Top Left new Vector2(0f, 1f)        | new Vector2(0f, 1f)   | Origin at top-left; positive X goes right, negative Y goes down.
    // Top Middle new Vector2(0.5f, 1f)    | new Vector2(0.5f, 1f) | Center-top of screen; perfectly centered horizontally.
    // Top Right new Vector2(1f, 1f)       | new Vector2(1f, 1f)   | Top-right; negative X moves left, negative Y moves down.
    // Middle Bottom new Vector2(0.5f, 0f) | new Vector2(0.5f, 0f) | Center-bottom (standard for health/stamina); positive Y moves up.
    // Bottom Right new Vector2(1f, 0f)    | new Vector2(1f, 0f)   | Bottom-right corner; negative X moves left, positive Y moves up.
    private static readonly Dictionary<BarLocation, BarData> BarLocations = new Dictionary<BarLocation, BarData>
    {
        { BarLocation.Hotbar, new BarData
        {
            AnchorMinMax = new Vector2(0f, 1f), AnchorPivot = new Vector2(0f, 1f), AnchorPosition = new Vector2(50f, -200f)
        } },
        { BarLocation.TopMiddle, new BarData
        {
            AnchorMinMax = new Vector2(0.5f, 1f), AnchorPivot = new Vector2(0.5f, 1f), AnchorPosition = new Vector2(0f, -120f)
        } },
        { BarLocation.Minimap, new BarData
        {
            AnchorMinMax = new Vector2(1f, 1f), AnchorPivot = new Vector2(1f, 1f), AnchorPosition = new Vector2(-100f, -360f)
        } },
        { BarLocation.Normal, new BarData
        {
            AnchorMinMax = new Vector2(0.5f, 0f), AnchorPivot = new Vector2(0.5f, 0f), AnchorPosition = new Vector2(0f, 220f)
        } },
        { BarLocation.BottomRight, new BarData
        {
            AnchorMinMax = new Vector2(1f, 0f), AnchorPivot = new Vector2(1f, 0f), AnchorPosition = new Vector2(-100f, 200f)
        } },
    };

    // Plugin Info
    private const string ModGuid = "jawlessjman.RageNAdrenaline";
    public const string ModName = "RageNAdrenaline";
    public const string ModVersion = "1.0.0";
    
    private readonly Dictionary<string, GuiBar> _guiBars = new Dictionary<string, GuiBar>();
    
    // Configuration options
    public static ConfigEntry<BarLocation> BarLocationConfig;
        
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        
        BindConfig();
        
        GUIManager.OnCustomGUIAvailable += AddCustomBars;
        
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }
    
    private void BindConfig()
    {
        BarLocationConfig = Config.Bind(
            "General", 
            "BarLocation", 
            BarLocation.Hotbar, 
            new ConfigDescription("Sets the location of the Adrenaline and Rage bars")
            );
    }

    private void AddCustomBars()
    {
        _guiBars.Clear();
        AddCustomBar("Adrenaline", Adrenaline.GetAdrenalineLevel(), Adrenaline.GetMaxAdrenalineLevel(), Color.green, BarLocationConfig.Value, Vector2.zero, new Vector2(220f, 40f));
        AddCustomBar("Rage", Rage.GetRageLevel(), Rage.GetMaxRageLevel(), Color.red, BarLocationConfig.Value, new Vector2(0f, 40f), new Vector2(220f, 40f));
    }

    private void Update()
    {
        if (Player.m_localPlayer == null) return;
        
        Adrenaline.AddAdrenaline(Adrenaline.GetAdrenalineRegenRate() * Time.deltaTime);
        
        Rage.AddRage(Rage.GetRageRegenRate() * Time.deltaTime);
        
        // Logger.LogInfo($"Adrenaline: {Adrenaline.GetAdrenalineLevel()} / {Adrenaline.GetMaxAdrenalineLevel()}");
        // Logger.LogInfo($"Rage: {Rage.GetRageLevel()} / {Rage.GetMaxRageLevel()}");
        
        UpdateVisualBars();
    }

    private void UpdateVisualBars()
    {
        foreach (var guiBar in _guiBars.Where(guiBar => guiBar.Value != null))
        {
            if (guiBar.Key.ToLower().Contains("adrenaline"))
            {
                guiBar.Value.SetValue(Adrenaline.GetAdrenalineLevel());
            }
            if (guiBar.Key.ToLower().Contains("rage"))
            {
                guiBar.Value.SetValue(Rage.GetRageLevel());
            }
        }
    }

    private void AddCustomBar(string barName, float defaultValue, float defaultMaxValue, Color barColor, BarLocation barLocation, Vector2 anchorPositionOffset, Vector2 sizeDelta)
    {
        var custom = new GameObject(barName, typeof(RectTransform));
        custom.transform.SetParent(GUIManager.CustomGUIFront.transform, false); // Changed to back instead of front
        
        var rect = custom.GetComponent<RectTransform>();
        
        var barData = BarLocations[barLocation];
        
        rect.anchorMin = barData.AnchorMinMax;
        rect.anchorMax = barData.AnchorMinMax;
        
        // Temp pivot location
        rect.pivot = barData.AnchorPivot;
        
        rect.anchoredPosition = barData.AnchorPosition + anchorPositionOffset;
        rect.sizeDelta = sizeDelta;

        if (Hud.instance == null)
        {
            Logger.LogWarning("Hud.instance is null");
            return;
        }

        var slowObj = Instantiate(Hud.instance.m_adrenalineBarSlow.gameObject, custom.transform);
        var fastObj = Instantiate(Hud.instance.m_adrenalineBarFast.gameObject, custom.transform);

        slowObj.name = $"{barName}BarSlow";
        fastObj.name = $"{barName}BarFast";

        var slowBar = slowObj.GetComponent<GuiBar>();
        var fastBar = fastObj.GetComponent<GuiBar>();

        slowBar.m_barImage.color = barColor;
        fastBar.m_barImage.color = barColor;
        
        var slowRect = slowBar.GetComponent<RectTransform>();
        var fastRect = fastBar.GetComponent<RectTransform>();

        slowRect.anchorMin = Vector2.zero;
        slowRect.anchorMax = Vector2.one;
        slowRect.pivot = new Vector2(0.5f, 0.5f);
        slowRect.anchoredPosition = Vector2.zero;
        slowRect.sizeDelta = Vector2.zero;

        fastRect.anchorMin = Vector2.zero;
        fastRect.anchorMax = Vector2.one;
        fastRect.pivot = new Vector2(0.5f, 0.5f);
        fastRect.anchoredPosition = Vector2.zero;
        fastRect.sizeDelta = Vector2.zero;

        slowBar.gameObject.SetActive(true);
        fastBar.gameObject.SetActive(true);

        slowBar.SetMaxValue(defaultMaxValue);
        fastBar.SetMaxValue(defaultMaxValue);

        slowBar.SetValue(defaultValue);
        fastBar.SetValue(defaultValue);

        _guiBars.Add(barName + "slow", slowBar);
        _guiBars.Add(barName + "fast", fastBar);
        
        Logger.LogInfo($"Slow source: {Hud.instance.m_adrenalineBarSlow}");
        Logger.LogInfo($"Fast source: {Hud.instance.m_adrenalineBarFast}");
        Logger.LogInfo($"Custom GUI Front: {GUIManager.CustomGUIFront}");
        
        Logger.LogInfo($"Added {barName} bar");
    }
}
