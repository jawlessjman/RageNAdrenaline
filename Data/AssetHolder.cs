using System.Collections.Generic;
using Jotunn.Utils;
using UnityEngine;

namespace RageNAdrenaline.Data;

public static class AssetHolder
{
    private static readonly Dictionary<string, AudioClip> AudioClips = new();
    private static readonly Dictionary<string, Sprite> Sprites = new();

    private static readonly Dictionary<string, string> AudioClipNames = new()
    {
        { "AdrenalineActivate", "assets/ragenadrenaline/sounds/adrenalineactivate.wav" },
        { "AdrenalineMajorLoss", "assets/ragenadrenaline/sounds/adrenalinemajorloss.wav" },
        { "FullAdrenaline", "assets/ragenadrenaline/sounds/fulladrenaline.wav" },
        { "FullRage", "assets/ragenadrenaline/sounds/fullrage.wav" },
        { "RageActivate", "assets/ragenadrenaline/sounds/rageactivate.wav" },
        { "RageEnd", "assets/ragenadrenaline/sounds/rageend.wav" }
    };

    private static readonly Dictionary<string, string> SpriteNames = new()
    {
        { "rage", "assets/ragenadrenaline/icons/rage.png" },
        { "adrenaline", "assets/ragenadrenaline/icons/adrenaline.png" }
    };

    private static AssetBundle _assetBundle;
    
    private static bool _loaded;
    
    public static AudioClip GetAudioClip(string name)
    {
        if (!_loaded) return null;
        return AudioClips.TryGetValue(name, out var audioClip) ? audioClip : null;
    }
    
    public static Sprite GetSprite(string name)
    {
        if (!_loaded) return null;
        return Sprites.TryGetValue(name, out var sprite) ? sprite : null;
    }

    public static void LoadAssetBundle()
    {
        if (_loaded) return;
        
        _assetBundle = AssetUtils.LoadAssetBundleFromResources($"{Plugin.ModName}.Assets.Bundles.ragenadrenaline");
        if (_assetBundle == null)
        {
            return;
        }
        
        foreach (var audioClipName in AudioClipNames)
        {
            var clip = _assetBundle.LoadAsset<AudioClip>(audioClipName.Value);

            if (clip == null)
            {
                continue;
            }
            AudioClips.Add(audioClipName.Key, clip);
        }
        
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