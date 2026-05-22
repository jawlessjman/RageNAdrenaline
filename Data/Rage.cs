namespace RageNAdrenaline.Data;

public static class Rage
{
    private static int _rageLevel = 0;
    private static int _maxRageLevel = 10;
    
    public static int GetRageLevel()
    {
        return _rageLevel;
    }
    
    public static void AddRage()
    {
        _rageLevel++;
        if (_rageLevel > _maxRageLevel) _rageLevel = _maxRageLevel;
    }
    
    public static void RemoveRage()
    {
        _rageLevel--;
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