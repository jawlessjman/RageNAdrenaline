namespace RageNAdrenaline.Data;

public static class Rage
{
    private static int _rageLevel = 100;
    private static int _maxRageLevel = 100;
    private static float _rageRegenRate = 1f;
    
    public static int GetRageLevel()
    {
        return _rageLevel;
    }
    
    public static float GetRageRegenRate()
    {
        return _rageRegenRate;
    }
    
    public static void AddRage(float value = 1)
    {
        _rageLevel += (int)value;
        if (_rageLevel > _maxRageLevel) _rageLevel = _maxRageLevel;
    }
    
    public static void RemoveRage(float value = 1)
    {
        _rageLevel -= (int)value;
        if (_rageLevel < 0) _rageLevel = 0;
    }
    
    public static void SetRageLevel(int level)
    {
        _rageLevel = level;
        if (_rageLevel > _maxRageLevel) _rageLevel = _maxRageLevel;
        
        if (_rageLevel < 0) _rageLevel = 0;
    }
    
    public static int GetMaxRageLevel()
    {
        return _maxRageLevel;
    }
    
    public static void SetMaxRageLevel(int level)
    {
        _maxRageLevel = level;
    }
}