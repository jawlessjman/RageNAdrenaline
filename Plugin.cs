using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Managers;
using RageNAdrenaline.Data;
using RageNAdrenaline.Data.Enums;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    private static readonly Dictionary<BarLocation, BarData> BarLocations = new()
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
    
    // Rage Distance modifiers
    private const float MinEnemyDistance = 0.1f;
    private const float MaxEnemyDistance = 50f;

    private static float _noEnemyTimer;
    private const float NoEnemyTimerThreshold = 10f;

    private const float MinMultiplier = 1f;
    private const float MaxMultiplier = 2f;

    private const float MaxBossRange = 200f;

    public static PowerMeter RageMeter = new(0, 100, 40, GetStatusEffect.RageDuration);
    public static PowerMeter AdrenalineMeter =  new(0, 100, 25, GetStatusEffect.AdrenalineDuration);

    // Plugin Info
    private const string ModGuid = "jawlessjman.RageNAdrenaline";
    public const string ModName = "RageNAdrenaline";
    public const string ModVersion = "1.0.0";
    
    private readonly Dictionary<string, GuiBar> _guiBars = new();
    private readonly Dictionary<string, TextMeshProUGUI> _barTexts = new();
    
    private Harmony _harmony;
    
    //Button configs
    private static ButtonConfig _rageButtonConfig;
    private static ButtonConfig _adrenalineButtonConfig;
    
    // Configuration options
    private static ConfigEntry<BarLocation> _barLocationConfig;

    private static ConfigEntry<KeyCode> _rageKeyConfig;
    private static ConfigEntry<InputManager.GamepadButton> _rageControllerConfig;

    private static ConfigEntry<KeyCode> _adrenalineKeyConfig;
    private static ConfigEntry<InputManager.GamepadButton> _adrenalineControllerConfig;
        
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        
        BindConfig();
        
        AddInputs();
        
        // Load local translation for English
        var assembly = Assembly.GetExecutingAssembly();

        const string resourceName = $"{ModName}.Assets.Translations.English.RageNAdrenaline.json";
        
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            Logger.LogError($"Resource: {resourceName} to load English translation file");
        }
        else
        {
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            LocalizationManager.Instance.GetLocalization().AddJsonFile("English", json);
        }
        
        AssetHolder.LoadAssetBundle();
        
        ItemManager.OnItemsRegistered += GetStatusEffect.RegisterStatusEffects;
        
        GUIManager.OnCustomGUIAvailable += AddCustomBars;
        
        _harmony = new Harmony(ModGuid);
        _harmony.PatchAll();
        
        Logger.LogInfo($"Plugin {ModName}-{ModVersion} is loaded!");
    }

    public static void LoadPowerStatusEffects()
    {
        var rageEffect = GetStatusEffect.GetPowerStatusEffect("rage");
        if (rageEffect != null)
        {
            RageMeter.StatusEffect = rageEffect;
        }
        RageMeter.FullSound = AssetHolder.GetAudioClip("FullRage");
        RageMeter.EndSound = AssetHolder.GetAudioClip("RageEnd");

        var adrenalineEffect = GetStatusEffect.GetPowerStatusEffect("adrenaline");
        if (adrenalineEffect != null)
        {
            AdrenalineMeter.StatusEffect = adrenalineEffect;
        }
        
        AdrenalineMeter.FullSound = AssetHolder.GetAudioClip("FullAdrenaline");
    }
    
    private void BindConfig()
    {
        _barLocationConfig = Config.Bind(
            "General", 
            "BarLocation", 
            BarLocation.Hotbar, 
            new ConfigDescription("Sets the location of the Adrenaline and Rage bars")
            );

        _adrenalineKeyConfig = Config.Bind(
            "Keybinds",
            "AdrenalineKey",
            KeyCode.F,
            new ConfigDescription("Keybind for Adrenaline bar activation")
            );
        
        _adrenalineControllerConfig = Config.Bind(
            "Keybinds",
            "AdrenalineController",
            InputManager.GamepadButton.DPadUp,
            new ConfigDescription("Controller button for Adrenaline bar activation")
            );
            
        _rageKeyConfig = Config.Bind(
            "Keybinds",
            "RageKey",
            KeyCode.G,
            new ConfigDescription("Keybind for Rage bar activation")
            );
        
        _rageControllerConfig = Config.Bind(
            "Keybinds",
            "RageController",
            InputManager.GamepadButton.DPadLeft,
            new ConfigDescription("Controller button for Rage bar activation")
            );
    }

    private static void HandleInputs()
    {
        if (ZInput.instance == null) return;

        if (ZInput.GetButtonDown(_rageButtonConfig.Name))
        {
            if (RageMeter.StatusEffect == null)
            {
                Logger.LogWarning("Rage status effect is null");
                return;
            }

            if (!RageMeter.HasMaxPower())
            {
                Logger.LogInfo("Rage status effect is not maxed");
                return;
            }
            RageMeter.Activate();
            var sound = AssetHolder.GetAudioClip("RageActivate");
            if (sound != null)
            {
                AudioSource.PlayClipAtPoint(sound, Player.m_localPlayer.transform.position);
            }
            Player.m_localPlayer.m_seman.AddStatusEffect(RageMeter.StatusEffect);
        }
        else if (ZInput.GetButtonDown(_adrenalineButtonConfig.Name))
        {
            if (AdrenalineMeter.StatusEffect == null)
            {
                Logger.LogWarning("Adrenaline status effect is null");
                return;
            }

            if (!AdrenalineMeter.HasMaxPower())
            {
                Logger.LogInfo("Adrenaline status effect is not maxed");
                return;
            }
            AdrenalineMeter.Activate();
            var sound = AssetHolder.GetAudioClip("AdrenalineActivate");
            if (sound != null)
            {
                AudioSource.PlayClipAtPoint(sound, Player.m_localPlayer.transform.position);
            }
            Player.m_localPlayer.m_seman.AddStatusEffect(AdrenalineMeter.StatusEffect);
        }
    }

    private static void AddInputs()
    {
        _rageButtonConfig = new ButtonConfig()
        {
            Name = "RageActivationButton",
            Config = _rageKeyConfig,
            GamepadConfig = _rageControllerConfig,
            HintToken = "$rage_activation_key",
            BlockOtherInputs = false
        };
        
        InputManager.Instance.AddButton(ModGuid, _rageButtonConfig);
        
        _adrenalineButtonConfig = new ButtonConfig()
        {
            Name = "AdrenalineActivationButton",
            Config = _adrenalineKeyConfig,
            GamepadConfig = _adrenalineControllerConfig,
            HintToken = "$adrenaline_activation_key",
            BlockOtherInputs = false
        };
        
        InputManager.Instance.AddButton(ModGuid, _adrenalineButtonConfig);
    }

    private void AddCustomBars()
    {
        _guiBars.Clear();
        _barTexts.Clear();
        
        var barSize = new Vector2(220f, 50f);
        
        AddCustomBar("Adrenaline", AdrenalineMeter.GetValue(), AdrenalineMeter.GetMaxValue(), Color.green, _barLocationConfig.Value, Vector2.zero, barSize);
        AddCustomBar("Rage", RageMeter.GetValue(), RageMeter.GetMaxValue(), Color.red, _barLocationConfig.Value, new Vector2(0f, 70f), barSize);
    }

    private void Update()
    {
        if (Player.m_localPlayer == null) return;
        
        AdrenalineMeter.AddPower();
        AdrenalineMeter.RemovePower();
        RageMeter.AddPower();
        RageMeter.RemovePower();
        
        UpdateVisualBars();
        
        HandleInputs();
    }

    private void FixedUpdate()
    {
        CheckNearbyEnemies();
    }

    private static void CheckNearbyEnemies()
    {
        var closestDistance = float.MaxValue;
        var player = Player.m_localPlayer;
        if (player == null) return;
        if (RageMeter.IsActive())
        {
            RageMeter.SetShouldRegen(false);
            RageMeter.SetShouldLose(true);
            return;
        }
        
        foreach (var character in from character in Character.GetAllCharacters() where character != null where character != player where !character.IsPlayer() where !character.IsDead() select character)
        {
            if (!BaseAI.IsEnemy(player, character)) continue;
            if (character.IsTamed()) continue;
            var distance = Vector3.Distance(player.transform.position, character.transform.position);
            if (character.IsBoss())
            {
                AdrenalineMeter.SetShouldRegen(!(distance >= MaxBossRange));
                if (distance >= MaxBossRange)
                {
                    AdrenalineMeter.ResetValue();
                }
            }
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
            }
        }

        if (closestDistance > MaxEnemyDistance)
        {
            _noEnemyTimer += Time.fixedDeltaTime;
            RageMeter.SetShouldRegen(false);
            RageMeter.ResetRegenRate();
            if (!(_noEnemyTimer >= NoEnemyTimerThreshold)) return;
            RageMeter.SetShouldLose(true);
        }
        else
        {
            _noEnemyTimer = 0f;
            var dist = Mathf.InverseLerp(MaxEnemyDistance, MinEnemyDistance, closestDistance);
            var mod = Mathf.Lerp(MinMultiplier, MaxMultiplier, dist);
        
            RageMeter.SetShouldRegen(true);
            RageMeter.SetShouldLose(false);
            RageMeter.SetRegenRateMultiplier(mod);
        }
    }

    private void UpdateVisualBars()
    {
        foreach (var guiBar in _guiBars.Where(guiBar => guiBar.Value != null))
        {
            if (guiBar.Key.ToLower().Contains("adrenaline"))
            {
                guiBar.Value.SetValue(AdrenalineMeter.GetValue());
            }
            if (guiBar.Key.ToLower().Contains("rage"))
            {
                guiBar.Value.SetValue(RageMeter.GetValue());
            }
        }
        
        foreach (var barText in _barTexts)
        {
            if (barText.Key.ToLower().Contains("adrenaline"))
            {
                barText.Value.text = $"{AdrenalineMeter.GetValue():0}";
            }
            if (barText.Key.ToLower().Contains("rage"))
            {
                barText.Value.text = $"{RageMeter.GetValue():0}";
            }
        }
    }

    private void AddCustomBar(string barName, float defaultValue, float defaultMaxValue, Color barColor, BarLocation barLocation, Vector2 anchorPositionOffset, Vector2 sizeDelta)
    {
        if (Hud.instance == null) return;
        if (GUIManager.CustomGUIFront == null) return;
        
        var custom = new GameObject(barName, typeof(RectTransform));
        custom.transform.SetParent(GUIManager.CustomGUIFront.transform, false);
        
        var rect = custom.GetComponent<RectTransform>();
        
        var barData = BarLocations[barLocation];
        
        rect.anchorMin = barData.AnchorMinMax;
        rect.anchorMax = barData.AnchorMinMax;
        
        // Temp pivot location
        rect.pivot = barData.AnchorPivot;
        
        rect.anchoredPosition = barData.AnchorPosition + anchorPositionOffset;
        rect.sizeDelta = sizeDelta;
        
        var borderObj = new GameObject($"{barName}Border", typeof(RectTransform), typeof(Image));
        borderObj.transform.SetParent(custom.transform, false);

        var borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0.5f, 0.5f);
        borderRect.anchorMax = new Vector2(0.5f, 0.5f);
        borderRect.pivot = new Vector2(0.5f, 0.5f);
        borderRect.anchoredPosition = Vector2.zero;

        var borderImage = borderObj.GetComponent<Image>();
        borderImage.color = new Color(0f, 0f, 0f, 0.4f);
        borderImage.raycastTarget = false;

        var slowObj = Instantiate(Hud.instance.m_adrenalineBarSlow.gameObject, custom.transform);
        var fastObj = Instantiate(Hud.instance.m_adrenalineBarFast.gameObject, custom.transform);

        slowObj.name = $"{barName}BarSlow";
        fastObj.name = $"{barName}BarFast";

        var slowBar = slowObj.GetComponent<GuiBar>();
        var fastBar = fastObj.GetComponent<GuiBar>();
        
        fastBar.m_barImage.color = barColor;

        slowBar.m_changeDelay = 0.1f;
        slowBar.m_smoothDrain = true;
        slowBar.m_smoothFill = true;
        slowBar.m_smoothSpeed = 1f;
        slowBar.m_smoothValue = 1f;
        
        fastBar.m_smoothDrain = false;
        fastBar.m_smoothFill = false;
        
        var slowRect = slowBar.GetComponent<RectTransform>();
        var fastRect = fastBar.GetComponent<RectTransform>();

        slowBar.m_barImage.rectTransform.sizeDelta =
            new Vector2(sizeDelta.x, slowBar.m_barImage.rectTransform.sizeDelta.y);
        fastBar.m_barImage.rectTransform.sizeDelta = new Vector2(sizeDelta.x, fastBar.m_barImage.rectTransform.sizeDelta.y);
        
        borderRect.sizeDelta = new Vector2(fastBar.m_barImage.rectTransform.sizeDelta.x * 1.05f, fastBar.m_barImage.rectTransform.sizeDelta.y * 1.5f);
        
        var textObj = new GameObject($"{barName}Text", typeof(RectTransform));
        textObj.transform.SetParent(custom.transform, false);

        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = $"{defaultValue:0}";
        text.fontSize = 18;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        text.font = Hud.instance.m_hoverName.font;

        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;

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
        
        _barTexts.Add(barName, text);
        
        Logger.LogInfo($"Slow source: {Hud.instance.m_adrenalineBarSlow}");
        Logger.LogInfo($"Fast source: {Hud.instance.m_adrenalineBarFast}");
        Logger.LogInfo($"Custom GUI Front: {GUIManager.CustomGUIFront}");
        
        Logger.LogInfo($"Added {barName} bar");
    }
}
