using UnityEngine;

namespace RageNAdrenaline.Data;

public class PowerMeter
{
    public StatusEffect StatusEffect { get; set; }

    private float _value;
    private readonly float _maxValue;
    
    public AudioClip FullSound { get; set;}
    public AudioClip EndSound { get; set;}

    private readonly float _baseRegenRate;
    private float _regenRate;

    private readonly float _lossRate;
    
    private bool _playedFullSound;

    private bool _shouldRegen;
    private bool _shouldLose;
    private bool _isActive;

    public PowerMeter(float startValue, float maxValue, float timeToFull, float duration)
    {
        _value = startValue;
        _maxValue = maxValue;

        _baseRegenRate = maxValue / timeToFull;
        _regenRate = _baseRegenRate;

        _lossRate = maxValue / duration;
    }

    public void ResetRegenRate()
    {
        _regenRate = _baseRegenRate;
    }

    public void SetRegenRateMultiplier(float multiplier)
    {
        _regenRate = _baseRegenRate * multiplier;
    }

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

    public void ResetValue()
    {
        //_value = 0f;
        _isActive = false;
        _shouldRegen = false;
        _shouldLose = true;
        _playedFullSound = false;
        _regenRate = _baseRegenRate;
    }

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

    public float GetValue() => _value;
    public float GetMaxValue() => _maxValue;
    public float GetRegenRate() => _regenRate;
    public float GetLossRate() => _lossRate;

    public void SetPower(float value)
    {
        _value = Mathf.Clamp(value, 0f, _maxValue);
    }

    public bool HasMaxPower() => _value >= _maxValue;
    public bool IsActive() => _isActive;
    public bool ShouldRegen() => _shouldRegen;
    public bool ShouldLose() => _shouldLose;

    public void SetShouldRegen(bool value) => _shouldRegen = value;
    public void SetShouldLose(bool value) => _shouldLose = value;

    public void Activate()
    {
        _isActive = true;
        _shouldRegen = false;
        _shouldLose = true;
    }
}