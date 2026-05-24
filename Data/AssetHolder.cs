using System.Collections.Generic;
using Jotunn.Utils;
using UnityEngine;

namespace RageNAdrenaline.Data;

/// <summary>
/// Holds all the assets used by the mod
/// </summary>
public static class AssetHolder
{
    /// <summary>
    /// Audio clips and sprites
    /// </summary>
    private static readonly Dictionary<string, AudioClip> AudioClips = new();
    /// <summary>
    /// Audio clips and sprites
    /// </summary>
    private static readonly Dictionary<string, Sprite> Sprites = new();

    /// <summary>
    /// Audio clip names and paths
    /// </summary>
    private static readonly Dictionary<string, string> AudioClipNames = new()
    {
        { "AdrenalineActivate", "assets/ragenadrenaline/sounds/adrenalineactivate.wav" },
        { "AdrenalineMajorLoss", "assets/ragenadrenaline/sounds/adrenalinemajorloss.wav" },
        { "FullAdrenaline", "assets/ragenadrenaline/sounds/fulladrenaline.wav" },
        { "FullRage", "assets/ragenadrenaline/sounds/fullrage.wav" },
        { "RageActivate", "assets/ragenadrenaline/sounds/rageactivate.wav" },
        { "RageEnd", "assets/ragenadrenaline/sounds/rageend.wav" }
    };

    /// <summary>
    /// Sprite names and paths
    /// </summary>
    private static readonly Dictionary<string, string> SpriteNames = new()
    {
        { "rage", "assets/ragenadrenaline/icons/rage.png" },
        { "adrenaline", "assets/ragenadrenaline/icons/adrenaline.png" }
    };
    
    /// <summary>
    /// The asset bundle containing all the assets
    /// </summary>
    private static AssetBundle _assetBundle;
    
    /// <summary>
    /// Whether the assets have been loaded
    /// </summary>
    private static bool _loaded;
    
    /// <summary>
    /// Gets an audio clip by name
    /// </summary>
    /// <param name="name">Name of the audio clip to retrieve</param>
    /// <returns>The audio clip if found, otherwise null</returns>
    public static AudioClip GetAudioClip(string name)
    {
        if (!_loaded) return null;
        return AudioClips.TryGetValue(name, out var audioClip) ? audioClip : null;
    }
    
    /// <summary>
    /// Gets a sprite by name
    /// </summary>
    /// <param name="name">Name of the sprite to retrieve</param>
    /// <returns>The sprite if found, otherwise null</returns>
    public static Sprite GetSprite(string name)
    {
        if (!_loaded) return null;
        return Sprites.TryGetValue(name, out var sprite) ? sprite : null;
    }

    /// <summary>
    /// Loads the asset bundle containing all the assets
    /// </summary>
    public static void LoadAssetBundle()
    {
        if (_loaded) return;
        
        // Get the asset bundle
        _assetBundle = AssetUtils.LoadAssetBundleFromResources($"{Plugin.ModName}.Assets.Bundles.ragenadrenaline");
        if (_assetBundle == null)
        {
            Plugin.Logger.LogError("Failed to load asset bundle");
            return;
        }

        foreach (var asset in _assetBundle.GetAllAssetNames())
        {
            Plugin.Logger.LogDebug($"Loaded asset: {asset}");
        }
        
        // Load the audio clips 
        foreach (var audioClipName in AudioClipNames)
        {
            var clip = _assetBundle.LoadAsset<AudioClip>(audioClipName.Value);

            if (clip == null)
            {
                continue;
            }
            AudioClips.Add(audioClipName.Key, clip);
        }
        
        // Load the sprites
        foreach (var spriteName in SpriteNames)
        {
            var sprite = _assetBundle.LoadAsset<Sprite>(spriteName.Value);
            if (sprite == null)
            {
                continue;
            }
            Sprites.Add(spriteName.Key, sprite);
        }
        
        _loaded = true;
    }
}