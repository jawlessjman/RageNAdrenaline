using UnityEngine;

namespace RageNAdrenaline.Data;

/// <summary>
/// Manages the power meter
/// </summary>
public class PowerMeter
{
    /// <summary>
    /// The status effect that is being applied
    /// </summary>
    public StatusEffect StatusEffect { get; set; }

    /// <summary>
    /// The current value of the power meter
    /// </summary>
    private float _value;
    /// <summary>
    /// The maximum value of the power meter
    /// </summary>
    private readonly float _maxValue;
    
    /// <summary>
    /// The sounds to play when the power meter is full
    /// </summary>
    public AudioClip FullSound { get; set;}
    /// <summary>
    /// The sounds to play when the status effect ends
    /// </summary>
    public AudioClip EndSound { get; set;}

    /// <summary>
    /// The default rate at which the power meter regenerates
    /// </summary>
    private readonly float _baseRegenRate;
    /// <summary>
    /// The rate at which the power meter regenerates
    /// </summary>
    private float _regenRate;

    /// <summary>
    /// The rate at which the power meter loses power
    /// </summary>
    private readonly float _lossRate;
    
    /// <summary>
    /// Whether the full sound has been played
    /// </summary>
    private bool _playedFullSound;

    /// <summary>
    /// Whether the power meter should regenerate
    /// </summary>
    private bool _shouldRegen;
    /// <summary>
    /// Whether the power meter should lose power
    /// </summary>
    private bool _shouldLose;
    /// <summary>
    /// Whether the power meter is active
    /// </summary>
    private bool _isActive;

    /// <summary>
    /// Creates a new power meter
    /// </summary>
    /// <param name="startValue">The starting value of the power meter</param>
    /// <param name="maxValue">The maximum value of the power meter</param>
    /// <param name="timeToFull">The time it takes for the power meter to reach full capacity</param>
    /// <param name="duration">The duration of the power meter's effect</param>
    public PowerMeter(float startValue, float maxValue, float timeToFull, float duration)
    {
        _value = startValue;
        _maxValue = maxValue;

        _baseRegenRate = maxValue / timeToFull;
        _regenRate = _baseRegenRate;

        _lossRate = maxValue / duration;
    }

    /// <summary>
    /// Resets the regen rate to the default rate
    /// </summary>
    public void ResetRegenRate()
    {
        _regenRate = _baseRegenRate;
    }

    /// <summary>
    /// Sets the regen rate multiplier
    /// </summary>
    /// <param name="multiplier">The multiplier to apply to the regen rate</param>
    public void SetRegenRateMultiplier(float multiplier)
    {
        _regenRate = _baseRegenRate * multiplier;
    }

    /// <summary>
    /// Adds power to the power meter
    /// </summary>
    public void AddPower()
    {
        if (!_shouldRegen) return;
        if (_isActive) return;

        _value += _regenRate * Time.deltaTime;
        _value = Mathf.Clamp(_value, 0f, _maxValue);

        if (!(_value >= _maxValue) || _playedFullSound) return;
        _playedFullSound = true;
        if (FullSound != null)
        {
            AudioSource.PlayClipAtPoint(FullSound, Player.m_localPlayer.transform.position);
        }
    }

    /// <summary>
    /// Resets the power meter to its starting values
    /// </summary>
    public void ResetValue()
    {
        _isActive = false;
        _shouldRegen = false;
        _shouldLose = true;
        _playedFullSound = false;
        _regenRate = _baseRegenRate;
    }

    /// <summary>
    /// Removes power from the power meter
    /// </summary>
    public void RemovePower()
    {
        if (!_shouldLose) return;

        _value -= _lossRate * Time.deltaTime;
        _value = Mathf.Clamp(_value, 0f, _maxValue);

        if (_value < _maxValue)
        {
            _playedFullSound = false;
        }

        if (_value > 0f) return;

        var wasActive = _isActive;

        _value = 0f;
        _isActive = false;
        _shouldLose = false;
        _playedFullSound = false;

        if (wasActive && EndSound != null && Player.m_localPlayer != null)
        {
            AudioSource.PlayClipAtPoint(EndSound, Player.m_localPlayer.transform.position);
        }
    }

    /// <summary>
    /// Gets the current value of the power meter
    /// </summary>
    /// <returns>Current power value</returns>
    public float GetValue() => _value;
    
    /// <summary>
    /// Gets the maximum value of the power meter
    /// </summary>
    /// <returns>Max value</returns>
    public float GetMaxValue() => _maxValue;
    /// <summary>
    /// Gets the regen rate of the power meter
    /// </summary>
    /// <returns>Regen rate</returns>
    public float GetRegenRate() => _regenRate;
    /// <summary>
    /// Gets the loss rate of the power meter
    /// </summary>
    /// <returns>Loss rate</returns>
    public float GetLossRate() => _lossRate;

    /// <summary>
    /// Sets the power meter's value'
    /// </summary>
    /// <param name="value">The value to set</param>
    public void SetPower(float value)
    {
        _value = Mathf.Clamp(value, 0f, _maxValue);
    }

    /// <summary>
    /// Whether the power meter has reached its maximum value
    /// </summary>
    /// <returns>If the power meter has reached its maximum value</returns>
    public bool HasMaxPower() => _value >= _maxValue;
    /// <summary>
    /// Whether the power meter is active
    /// </summary>
    /// <returns>if the power meter is active</returns>
    public bool IsActive() => _isActive;
    /// <summary>
    /// Whether the power meter should regenerate
    /// </summary>
    /// <returns>If the power meter should regenerate</returns>
    public bool ShouldRegen() => _shouldRegen;
    /// <summary>
    /// Whether the power meter should lose power
    /// </summary>
    /// <returns>If the power meter should lose power</returns>
    public bool ShouldLose() => _shouldLose;

    /// <summary>
    /// Sets the power meters should regenerate and should lose power values'
    /// </summary>
    /// <param name="value">If the bar should be regenerating</param>
    public void SetShouldRegen(bool value) => _shouldRegen = value;
    /// <summary>
    /// Sets the power meters should regenerate and should lose power values'
    /// </summary>
    /// <param name="value">If the bar should be losing power</param>
    public void SetShouldLose(bool value) => _shouldLose = value;

    /// <summary>
    /// Activates the power meter
    /// </summary>
    public void Activate()
    {
        _isActive = true;
        _shouldRegen = false;
        _shouldLose = true;
    }
}