using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using RageNAdrenaline.Data.Enums;

namespace RageNAdrenaline;

[BepInPlugin(_modGuid, ModName, ModVersion)]
[BepInDependency(Jotunn.Main.ModGuid)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    
    // Plugin Info
    private const string _modGuid = "jawlessjman.RageNAdrenaline";
    public const string ModName = "RageNAdrenaline";
    public const string ModVersion = "1.0.0";
    
    // Configuration options
    public static ConfigEntry<BarLocation> BarLocation;
    
    // Harmony
    private Harmony _harmony;
        
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        
        BindConfig();
        
        _harmony = new Harmony(_modGuid);
        _harmony.PatchAll();
    }
    
    
    private void BindConfig()
    {
        BarLocation = Config.Bind(
            "General", 
            "BarLocation", 
            Data.Enums.BarLocation.Hotbar, 
            new ConfigDescription("Sets the location of the Adrenaline and Rage bars")
            );
    }
}
