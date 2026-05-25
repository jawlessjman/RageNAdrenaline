using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace RageNAdrenaline.Data;

/// <summary>
/// Gets status effects from the game
/// </summary>
public static class GetStatusEffect
{
    /// <summary>
    /// Whether the status effects have been initialized
    /// </summary>
    private static bool _initialized;

    // Duration of the status effects
    public const float RageDuration = 9f;
    public const float AdrenalineDuration = 5f;
    
    /// <summary>
    /// All the status effects
    /// </summary>
    private static readonly Dictionary<string, CustomStatusEffect> StatusEffects = new();

    /// <summary>
    /// Gets a status effect by name
    /// </summary>
    /// <param name="name">name of the status effect</param>
    /// <returns></returns>
    public static StatusEffect GetPowerStatusEffect(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (ObjectDB.instance == null) return null;
        if (ObjectDB.instance.m_StatusEffects == null) return null;
        if (StatusEffects.Count == 0)
        {
            var seName = "SE_" + name;
            return ObjectDB.instance.GetStatusEffect(seName.GetStableHashCode());
        }
        
        return StatusEffects.TryGetValue(name, out var statusEffect) ? statusEffect.StatusEffect : null;
    }

    public static void ResetStatusEffects()
    {
        _initialized = false;
        StatusEffects.Clear();
    }
    
    /// <summary>
    /// Registers the status effects
    /// </summary>
    public static void RegisterStatusEffects()
    {
        if (_initialized) return;
        if (ObjectDB.instance == null) return;

        // Only register status effects if the config is the source of truth
        if (!Plugin.ConfigSync.IsSourceOfTruth) return;
        
        StatusEffects.Clear();
        
        CreateStatusEffects("rage", Plugin.RageDamageBoost.Value, RageDuration);
        CreateStatusEffects("adrenaline", Plugin.AdrenalineDamageBoost.Value, AdrenalineDuration);
        
        _initialized = true;
        Plugin.LoadPowerStatusEffects();
    }
    
    /// <summary>
    /// Creates a status effect
    /// </summary>
    /// <param name="seName">The name of the status effect</param>
    /// <param name="damageBoost">The damage boost of the status effect</param>
    /// <param name="duration">The duration of the status effect</param>
    private static void CreateStatusEffects(string seName, float damageBoost, float duration)
    {
        var effect = ScriptableObject.CreateInstance<SE_Stats>();
        
        effect.name = "SE_" + seName;
        effect.m_name = $"${seName}_effect";
        effect.m_startMessage = $"${seName}_start";
        effect.m_startMessageType = MessageHud.MessageType.Center;
        effect.m_ttl = duration;
        effect.m_icon = AssetHolder.GetSprite(seName) ?? Hud.instance.m_buildSnappingIcon;
        effect.m_tooltip = $"${seName}_tooltip";

        effect.m_percentigeDamageModifiers.Add(
            new HitData.DamageTypes
            {
                m_slash = damageBoost,
                m_pierce = damageBoost,
                m_blunt = damageBoost,
                m_fire = damageBoost,
                m_spirit = damageBoost,
                m_damage = damageBoost,
                m_chop = damageBoost,
                m_poison = damageBoost,
                m_frost = damageBoost,
                m_lightning = damageBoost,
                m_pickaxe = damageBoost
            }
        );
        
        var customEffect = new CustomStatusEffect(effect, false);
        ItemManager.Instance.AddStatusEffect(customEffect);
        StatusEffects.Add(seName, customEffect);
    }
}