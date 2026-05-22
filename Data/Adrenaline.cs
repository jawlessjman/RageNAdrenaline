namespace RageNAdrenaline.Data;

public static class Adrenaline
{
    private static int _adrenalineLevel = 0;
    private static int _maxAdrenalineLevel = 10;
    
    public static int GetAdrenalineLevel()
    {
        return _adrenalineLevel;
    }
    
    public static void AddAdrenaline()
    {
        _adrenalineLevel++;
        if (_adrenalineLevel > _maxAdrenalineLevel) _adrenalineLevel = _maxAdrenalineLevel;
    }
    
    public static void RemoveAdrenaline()
    {
        _adrenalineLevel--;
        if (_adrenalineLevel < 0) _adrenalineLevel = 0;
    }
    
    public static void SetAdrenalineLevel(int level)
    {
        _adrenalineLevel = level;
        if (_adrenalineLevel > _maxAdrenalineLevel) _adrenalineLevel = _maxAdrenalineLevel;
        if (_adrenalineLevel < 0) _adrenalineLevel = 0;
    }
    
    public static int GetMaxAdrenalineLevel()
    {
        return _maxAdrenalineLevel;
    }
    
    public static void SetMaxAdrenalineLevel(int level)
    {
        _maxAdrenalineLevel = level;
    }
}