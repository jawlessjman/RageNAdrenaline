using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace RageNAdrenaline.Data;

public static class GetStatusEffect
{
    private static bool _initialized;
    
    public static float RageDuration = 9f;
    public static float AdrenalineDuration = 5f;
    
    private static readonly Dictionary<string, CustomStatusEffect> StatusEffects = new();

    /// <summary>
    /// Gets a status effect by name
    /// </summary>
    /// <param name="name">name of the status effect</param>
    /// <returns></returns>
    public static StatusEffect GetPowerStatusEffect(string name)
    {
        if (!_initialized) return null;
        if (StatusEffects.Count == 0) return null;
        if (ObjectDB.instance == null) return null;
        if (string.IsNullOrEmpty(name)) return null;
        if (ObjectDB.instance.m_StatusEffects == null) return null;
        
        return StatusEffects.TryGetValue(name, out var statusEffect) ? statusEffect.StatusEffect : null;
    }

    public static void RegisterStatusEffects()
    {
        if (_initialized) return;
        if (ObjectDB.instance == null) return;
        
        StatusEffects.Clear();
        
        CreateStatusEffects("rage", 0.35f, RageDuration);
        CreateStatusEffects("adrenaline", 2.5f, AdrenalineDuration);
        
        _initialized = true;
        Plugin.LoadPowerStatusEffects();
    }
    
    private static void CreateStatusEffects(string seName, float damageBoost, float duration)
    {
        var effect = ScriptableObject.CreateInstance<SE_Stats>();
        
        effect.name = "SE_" + seName;
        effect.m_name = $"${seName}_effect";
        effect.m_startMessage = $"{seName}_start";
        effect.m_startMessageType = MessageHud.MessageType.Center;
        effect.m_ttl = duration;
        effect.m_icon = AssetHolder.GetSprite(seName) ?? Hud.instance.m_buildSnappingIcon;
        effect.m_tooltip = $"{seName}_tooltip";

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