namespace RageNAdrenaline.Data;

public static class Adrenaline
{
    private static float _adrenalineLevel = 100;
    private static float _maxAdrenalineLevel = 100;
    private static float _adrenalineRegenRate = 1f;
    
    public static float GetAdrenalineLevel()
    {
        return _adrenalineLevel;
    }
    
    public static float GetAdrenalineRegenRate()
    {
        return _adrenalineRegenRate;
    }
    
    public static void AddAdrenaline(float value = 1)
    {
        _adrenalineLevel += value;
        if (_adrenalineLevel > _maxAdrenalineLevel) _adrenalineLevel = _maxAdrenalineLevel;
        if (_adrenalineLevel < 0) _adrenalineLevel = 0;
    }
    
    public static void RemoveAdrenaline(float value = 1)
    {
        _adrenalineLevel -= value;
        if (_adrenalineLevel < 0) _adrenalineLevel = 0;
    }
    
    public static void SetAdrenalineLevel(float level)
    {
        _adrenalineLevel = level;
        if (_adrenalineLevel > _maxAdrenalineLevel) _adrenalineLevel = _maxAdrenalineLevel;
        if (_adrenalineLevel < 0) _adrenalineLevel = 0;
    }
    
    public static float GetMaxAdrenalineLevel()
    {
        return _maxAdrenalineLevel;
    }
    
    public static void SetMaxAdrenalineLevel(int level)
    {
        _maxAdrenalineLevel = level;
    }
}