using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Managers;
using Jotunn.Utils;
using RageNAdrenaline.Data;
using RageNAdrenaline.Data.Enums;
using UnityEngine;
using UnityEngine.UI;
using ServerSync;
using TMPro;

namespace RageNAdrenaline;

/// <summary>
/// Main plugin class
/// </summary>
[BepInPlugin(ModGuid, ModName, ModVersion)]
[BepInDependency(Jotunn.Main.ModGuid)]
public class Plugin : BaseUnityPlugin
{
    internal new static ManualLogSource Logger;
    
    /// <summary>
    /// Locations for the Rage and Adrenaline meters
    /// </summary>
    private static readonly Dictionary<BarLocation, BarData> BarLocations = new()
    {
        { BarLocation.Hotbar, new BarData
        {
            AnchorMinMax = new Vector2(0f, 1f), AnchorPivot = new Vector2(0f, 1f), AnchorPosition = new Vector2(815f, -75f)
        } },
        { BarLocation.TopMiddle, new BarData
        {
            AnchorMinMax = new Vector2(0.5f, 1f), AnchorPivot = new Vector2(0.5f, 1f), AnchorPosition = new Vector2(0f, -100f)
        } },
        { BarLocation.Minimap, new BarData
        {
            AnchorMinMax = new Vector2(1f, 1f), AnchorPivot = new Vector2(1f, 1f), AnchorPosition = new Vector2(-80f, -359f)
        } },
        { BarLocation.Normal, new BarData
        {
            AnchorMinMax = new Vector2(0.5f, 0f), AnchorPivot = new Vector2(0.5f, 0f), AnchorPosition = new Vector2(0f, 230f)
        } },
        { BarLocation.BottomRight, new BarData
        {
            AnchorMinMax = new Vector2(1f, 0f), AnchorPivot = new Vector2(1f, 0f), AnchorPosition = new Vector2(-190f, 110f)
        } },
        { BarLocation.BottomLeft, new BarData
        {
            AnchorMinMax = new Vector2(0f, 0f), AnchorPivot = new Vector2(0f, 0f), AnchorPosition = new Vector2(190f, 110f)
        } },
    };
    
    // Rage Distance modifiers
    private const float MinEnemyDistance = 0.1f;
    private const float MaxEnemyDistance = 50f;

    private static float _noEnemyTimer;
    private const float NoEnemyTimerThreshold = 5f;

    private const float MinMultiplier = 1f;
    private const float MaxMultiplier = 2f;

    private const float MaxBossRange = 100f;
    
    // Power meters
    public static readonly PowerMeter RageMeter = new(0, 100, 35, GetStatusEffect.RageDuration);
    public static readonly PowerMeter AdrenalineMeter =  new(0, 100, 25, GetStatusEffect.AdrenalineDuration);

    // Plugin Info
    private const string ModGuid = "jawlessjman.RageNAdrenaline";
    public const string ModName = "RageNAdrenaline";
    private const string ModVersion = "1.1.0";
    
    // Stored Bars
    private readonly Dictionary<string, GuiBar> _guiBars = new();
    private readonly Dictionary<string, TextMeshProUGUI> _barTexts = new();
    
    private Harmony _harmony;
    
    // Cached values
    private static bool _wasDead;
    
    // Button configs
    private static ButtonConfig _rageButtonConfig;
    private static ButtonConfig _adrenalineButtonConfig;
    
    // Configuration options
    private static ConfigEntry<BarLocation> _barLocationConfig;

    private static ConfigEntry<KeyCode> _rageKeyConfig;
    private static ConfigEntry<InputManager.GamepadButton> _rageControllerConfig;

    private static ConfigEntry<KeyCode> _adrenalineKeyConfig;
    private static ConfigEntry<InputManager.GamepadButton> _adrenalineControllerConfig;

    public static ConfigEntry<float> RageDamageBoost;
    public static ConfigEntry<float> AdrenalineDamageBoost;
    public static ConfigEntry<float> AdrenalineDamageReduction;

    public static readonly ConfigSync ConfigSync = new(ModGuid)
    {
        DisplayName = ModName,
        CurrentVersion = ModVersion,
        MinimumRequiredVersion = "1.1.0",
        IsLocked = true
    };
    
    private static ConfigEntry<bool> _showTutorial;
    private static GameObject _tutorialPanel;
    
    /// <summary>
    /// This method is called when the game starts.
    /// </summary>
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        
        BindConfig();
        
        // Load local translation for English
        const string resourceName = $"{ModName}.Assets.Translations.English.RageNAdrenaline.json";
        var englishLocalized = AssetUtils.LoadTextFromResources(resourceName);
        if (string.IsNullOrEmpty(englishLocalized))
        {
            Logger.LogError($"Failed to load English translation file: {resourceName}");
        }
        else
        {
            LocalizationManager.Instance.GetLocalization().AddJsonFile("English", englishLocalized);
        }
        
        // Load Assets
        AssetHolder.LoadAssetBundle();
        
        // Load status effects for adrenaline and rage
        ItemManager.OnItemsRegistered += GetStatusEffect.RegisterStatusEffects;
        
        SynchronizationManager.OnConfigurationSynchronized += (_, _) =>
        {
            Logger.LogInfo("Configuration synchronized with server.");
            
            GetStatusEffect.ResetStatusEffects();
            GetStatusEffect.RegisterStatusEffects();
        };
        
        _harmony = new Harmony(ModGuid);
        _harmony.PatchAll();

        if (IsDedicatedServer())
        {
            Logger.LogInfo($"Plugin {ModName}-{ModVersion} is loaded on Server!");
            return;
        }
        
        AddInputs();
        
        // Create the custom GUI bars for rage and adrenaline
        GUIManager.OnCustomGUIAvailable += AddCustomBars;
        GUIManager.OnCustomGUIAvailable += ShowTutorialGUI;
        
        Logger.LogInfo($"Plugin {ModName}-{ModVersion} is loaded!");
    }

    /// <summary>
    /// Loads the status effects for adrenaline and rage
    /// </summary>
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
    
    /// <summary>
    /// Binds the config options to the plugin
    /// </summary>
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
        
        RageDamageBoost = Config.Bind(
            "Damage",
            "RageDamageBoost",
            1.35f,
            new ConfigDescription("Boosts the damage dealt by Rage (1.35 is 135% damage)", new AcceptableValueRange<float>(0.01f, 5f))
            );
        ConfigSync.AddConfigEntry(RageDamageBoost).SynchronizedConfig = true;

        AdrenalineDamageBoost = Config.Bind(
            "Damage",
            "AdrenalineDamageBoost",
            2.5f,
            new ConfigDescription("Boosts the damage dealt by Adrenaline (2.5 is 250% damage)", new AcceptableValueRange<float>(0.01f, 5f))
            );
        ConfigSync.AddConfigEntry(AdrenalineDamageBoost).SynchronizedConfig = true;
        
        AdrenalineDamageReduction = Config.Bind(
            "Damage",
            "AdrenalineDamageReduction",
            0.5f,
            new ConfigDescription("Reduces the damage taken when your Adrenaline bar is full (0.5 is 50% damage reduction)", new AcceptableValueRange<float>(0.01f, 5f))
            );
        ConfigSync.AddConfigEntry(AdrenalineDamageReduction).SynchronizedConfig = true;
        
        _showTutorial = Config.Bind(
            "Tutorial",
            "ShowTutorial",
            true,
            new ConfigDescription("Shows a tutorial when the plugin is first loaded")
            );
    }
    
    /// <summary>
    /// Handles the inputs for the Adrenaline and Rage bars
    /// </summary>
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
    
    /// <summary>
    /// Adds the inputs for the Adrenaline and Rage bars
    /// </summary>
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
    
    /// <summary>
    /// Creates the custom GUI bars for the Adrenaline and Rage bars
    /// </summary>
    private void AddCustomBars()
    {
        _guiBars.Clear();
        _barTexts.Clear();
        
        RageMeter.ResetValue();
        AdrenalineMeter.ResetValue();
        
        var barSize = new Vector2(220f, 50f);
        
        AddCustomBar("Adrenaline", AdrenalineMeter.GetValue(), AdrenalineMeter.GetMaxValue(), Color.green, _barLocationConfig.Value, Vector2.zero, barSize);
        AddCustomBar("Rage", RageMeter.GetValue(), RageMeter.GetMaxValue(), Color.red, _barLocationConfig.Value, new Vector2(0f, 30f), barSize);
    }
    
    /// <summary>
    /// This is called every frame while the game is running.
    /// </summary>
    private void Update()
    {
        if (IsDedicatedServer()) return;
        if (Player.m_localPlayer == null) return;

        if (Player.m_localPlayer.IsDead())
        {
            if (!_wasDead)
            {
                AdrenalineMeter.ResetValue();
                RageMeter.ResetValue();
                
                _wasDead = true;
            }
        }
        
        _wasDead = false;
        
        // Update the power meters values
        AdrenalineMeter.AddPower();
        AdrenalineMeter.RemovePower();
        RageMeter.AddPower();
        RageMeter.RemovePower();
        
        // Update the power meter visuals
        UpdateVisualBars();
        
        // Handle inputs for the Adrenaline and Rage bars
        HandleInputs();
    }
    
    /// <summary>
    /// This is called every fixed framerate frame if the MonoBehaviour is enabled.
    /// </summary>
    private void FixedUpdate()
    {
        if (IsDedicatedServer()) return;
        CheckNearbyEnemies();
    }
    
    private static bool IsDedicatedServer()
    {
        return SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
    }
    
    /// <summary>
    /// Checks for nearby enemies and manages RageMeter regeneration and loss based on distance.
    /// </summary>
    private static void CheckNearbyEnemies()
    {
        var closestDistance = float.MaxValue;
        var hasBossInRange = false;
        var player = Player.m_localPlayer;
        
        var shouldAdrenalineRegen = false;
        var shouldAdrenalineLose = false;
        
        if (player == null) return;
        
        // Check for nearby enemies
        foreach (var character in from character in Character.GetAllCharacters() where character != null where character != player where !character.IsPlayer() where !character.IsDead() select character)
        {
            if (!BaseAI.IsEnemy(player, character)) continue; // If the character does not have enemy AI
            if (character.IsTamed()) continue; // If the character is tamed
            
            
            var distance = Vector3.Distance(player.transform.position, character.transform.position);

            if (character.IsBoss() && distance <= MaxBossRange) // If the character is a boss
            {
                hasBossInRange = true;
            }
            
            if (distance < closestDistance) // Get closest distance to enemy
            {
                closestDistance = distance;
            }
        }

        if (AdrenalineMeter.IsActive()) //if adrenaline is active
        {
            shouldAdrenalineLose = true;
        }
        else if (!hasBossInRange) // If there is no boss in range
        {
            shouldAdrenalineLose = true;
        }
        else // If there is a boss in range
        {
            if (!AdrenalineMeter.IsLosingPower()) // If adrenaline is not losing power
            {
                shouldAdrenalineRegen = true;
            }
        }
        
        AdrenalineMeter.SetShouldRegen(shouldAdrenalineRegen);
        AdrenalineMeter.SetShouldLose(shouldAdrenalineLose);

        if (RageMeter.IsActive()) return;
        if (closestDistance > MaxEnemyDistance) // If there is no enemy in range
        {
            _noEnemyTimer += Time.fixedDeltaTime; // Increase timer to deactivate RageMeter
            RageMeter.SetShouldRegen(false); // Stop regeneration on rage meter
            if (!(_noEnemyTimer >= NoEnemyTimerThreshold)) return;
            RageMeter.SetShouldLose(true); // Start loss on rage meter
        }
        else // If there is an enemy in range
        {
            _noEnemyTimer = 0f; // Reset timer
            var dist = Mathf.InverseLerp(MaxEnemyDistance, MinEnemyDistance, closestDistance); // Calculate distance multiplier
            var mod = Mathf.Lerp(MinMultiplier, MaxMultiplier, dist);
        
            RageMeter.SetRegenRateMultiplier(mod); // Set regeneration rate multiplier
            RageMeter.SetShouldRegen(true); // Start regeneration on rage meter
            RageMeter.SetShouldLose(false); // Stop loss on rage meter
        }
    }
    
    /// <summary>
    /// Updates the visual bars for Adrenaline and Rage
    /// </summary>
    private void UpdateVisualBars()
    {
        // Update the visual bars
        foreach (var guiBar in _guiBars.Where(guiBar => guiBar.Value != null))
        {
            if (guiBar.Key.ToLower().Contains("adrenaline")) // Update the Adrenaline bar
            {
                guiBar.Value.SetValue(AdrenalineMeter.GetValue());
            }
            if (guiBar.Key.ToLower().Contains("rage")) // Update the Rage bar
            {
                guiBar.Value.SetValue(RageMeter.GetValue());
            }
        }
        
        // Update the text values
        foreach (var barText in _barTexts)
        {
            if (barText.Key.ToLower().Contains("adrenaline")) // Update the Adrenaline text
            {
                barText.Value.text = $"{AdrenalineMeter.GetValue():0}";
            }
            if (barText.Key.ToLower().Contains("rage")) // Update the Rage text
            {
                barText.Value.text = $"{RageMeter.GetValue():0}";
            }
        }
    }
    
    /// <summary>
    /// Creates a custom GUI bar for the Adrenaline and Rage bars
    /// </summary>
    /// <param name="barName">Name of the bar</param>
    /// <param name="defaultValue">Starting value for the bar</param>
    /// <param name="defaultMaxValue">Maximum value for the bar</param>
    /// <param name="barColor">Colour of the bar</param>
    /// <param name="barLocation">Location of the bar on the HUD</param>
    /// <param name="anchorPositionOffset">Offset for the bar's anchor position</param>
    /// <param name="sizeDelta">Size delta for the bar's RectTransform</param>
    private void AddCustomBar(string barName, float defaultValue, float defaultMaxValue, Color barColor, BarLocation barLocation, Vector2 anchorPositionOffset, Vector2 sizeDelta)
    {
        if (Hud.instance == null) return;
        if (GUIManager.CustomGUIFront == null) return;
        
        // Create the custom panels for the bars
        var custom = new GameObject(barName, typeof(RectTransform));
        custom.transform.SetParent(GUIManager.CustomGUIFront.transform, false);
        
        var rect = custom.GetComponent<RectTransform>();
        
        var data = BarLocations.TryGetValue(barLocation, out var barData);
        if (!data) return;
        
        rect.anchorMin = barData.AnchorMinMax;
        rect.anchorMax = barData.AnchorMinMax;
        
        rect.pivot = barData.AnchorPivot;
        
        rect.anchoredPosition = barData.AnchorPosition + anchorPositionOffset;
        rect.sizeDelta = sizeDelta;
        
        // Create the border object for the bar
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

        // Create the bar objects for the bar
        var slowObj = Instantiate(Hud.instance.m_adrenalineBarSlow.gameObject, custom.transform);
        var fastObj = Instantiate(Hud.instance.m_adrenalineBarFast.gameObject, custom.transform);

        slowObj.name = $"{barName}BarSlow";
        fastObj.name = $"{barName}BarFast";

        var slowBar = slowObj.GetComponent<GuiBar>();
        var fastBar = fastObj.GetComponent<GuiBar>();
        
        fastBar.m_barImage.color = barColor;
        
        // Set the bar's values.
        // The slow bar is the grey bar that slowly drains as the fast bar drains
        slowBar.m_changeDelay = Hud.instance.m_adrenalineBarSlow.m_changeDelay;
        slowBar.m_smoothDrain = Hud.instance.m_adrenalineBarSlow.m_smoothDrain;
        slowBar.m_smoothFill = Hud.instance.m_adrenalineBarSlow.m_smoothFill;
        slowBar.m_smoothSpeed = Hud.instance.m_adrenalineBarSlow.m_smoothSpeed;
        slowBar.m_smoothValue = Hud.instance.m_adrenalineBarSlow.m_smoothValue;
        
        // The fast bar is the main bar that drains and fills fast and is the main colour
        fastBar.m_changeDelay = Hud.instance.m_adrenalineBarFast.m_changeDelay;
        fastBar.m_smoothDrain = Hud.instance.m_adrenalineBarFast.m_smoothDrain;
        fastBar.m_smoothFill = Hud.instance.m_adrenalineBarFast.m_smoothFill;
        fastBar.m_smoothSpeed = Hud.instance.m_adrenalineBarFast.m_smoothSpeed;
        fastBar.m_smoothValue = Hud.instance.m_adrenalineBarFast.m_smoothValue;
        
        var slowRect = slowBar.GetComponent<RectTransform>();
        var fastRect = fastBar.GetComponent<RectTransform>();
        
        // Position the slow and fast bars
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

        // Set the size of the bars
        slowBar.m_barImage.rectTransform.sizeDelta =
            new Vector2(sizeDelta.x, slowBar.m_barImage.rectTransform.sizeDelta.y);
        fastBar.m_barImage.rectTransform.sizeDelta = new Vector2(sizeDelta.x, fastBar.m_barImage.rectTransform.sizeDelta.y);
        
        // Set the size of the black border
        borderRect.sizeDelta = new Vector2(fastBar.m_barImage.rectTransform.sizeDelta.x * 1.05f, fastBar.m_barImage.rectTransform.sizeDelta.y * 1.5f);
        
        // Create the text object for the bar
        var textObj = new GameObject($"{barName}Text", typeof(RectTransform));
        textObj.transform.SetParent(custom.transform, false);

        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = $"{defaultValue:0}";
        text.fontSize = 18;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        text.font = Hud.instance.m_hoverName.font;
        
        // Place the text object in the correct position
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;

        // Make sure the bars are active
        slowBar.gameObject.SetActive(true);
        fastBar.gameObject.SetActive(true);
    
        // Set the default and max values for the bars
        slowBar.SetMaxValue(defaultMaxValue);
        fastBar.SetMaxValue(defaultMaxValue);

        slowBar.SetValue(defaultValue);
        fastBar.SetValue(defaultValue);
    
        
        // Add the bars to the list of bars
        _guiBars.Add(barName + "slow", slowBar);
        _guiBars.Add(barName + "fast", fastBar);
        
        _barTexts.Add(barName, text);
    }

    private void ShowTutorialGUI() { 
        if (!_showTutorial.Value) return; 
        if (GUIManager.Instance == null) return; 
        if (GUIManager.CustomGUIFront == null) return; 
        _tutorialPanel = GUIManager.Instance.CreateWoodpanel( 
            parent: GUIManager.CustomGUIFront.transform, 
            anchorMin: new Vector2(0.5f, 0.5f), 
            anchorMax: new Vector2(0.5f, 0.5f), 
            position: new Vector2(0f, 0f), 
            width: 850, 
            height: 600, 
            draggable: false 
            ); 
        
        GUIManager.Instance.CreateText( 
            text: "$tutorial_ragenadrenaline_title", 
            parent: _tutorialPanel.transform, 
            anchorMin: new Vector2(0.5f, 1f), 
            anchorMax: new Vector2(0.5f, 1f), 
            position: new Vector2(0f, -50f), 
            font: GUIManager.Instance.AveriaSerifBold, 
            fontSize: 30, 
            color: GUIManager.Instance.ValheimOrange, 
            outline: true, 
            outlineColor: Color.black, 
            width: 400f, 
            height: 40f, 
            addContentSizeFitter: false
            ); 
        
        GUIManager.Instance.CreateText(
            text: "$tutorial_ragenadrenaline_description",
            parent: _tutorialPanel.transform, 
            anchorMin: new Vector2(0.5f, 1f), 
            anchorMax: new Vector2(0.5f, 1f),
            position: new Vector2(0f, -250f), 
            font: GUIManager.Instance.AveriaSerifBold, 
            fontSize: 20, 
            color: GUIManager.Instance.ValheimOrange,
            outline: true, 
            outlineColor: Color.black,
            width: 600f,
            height: 300f, 
            addContentSizeFitter: false
            ); 
        
        // Create the button object
        var buttonObject = GUIManager.Instance.CreateButton( 
            text: "$tutorial_ragenadrenaline_button", 
            parent: _tutorialPanel.transform, 
            anchorMin: new Vector2(0.5f, 0.5f),
            anchorMax: new Vector2(0.5f, 0.5f), 
            position: new Vector2(0, -250f), 
            width: 250f, 
            height: 60f
            ); 
        
        buttonObject.SetActive(true); 
        // Add a listener to the button to close the panel again
        var button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(FinishTutorial);
        
    }

    private void FinishTutorial()
    {
        _showTutorial.Value = false;
        Config.Save();
        if (_tutorialPanel == null) return;
        Destroy(_tutorialPanel);
        _tutorialPanel = null;
    }
}
