
using RageNAdrenaline.Data.Enums;

namespace RageNAdrenaline.Data;

public static class LogString
{
    public static void LogMessage(string message, PrintLevel level = PrintLevel.Info)
    {
        switch (level)
        {
            case PrintLevel.Info:
                Plugin.Logger.LogInfo(message);
                break;
            case PrintLevel.Warning:
                Plugin.Logger.LogWarning(message);
                break;
            case PrintLevel.Error:
                Plugin.Logger.LogError(message);
                break;
            default:
                Plugin.Logger.LogInfo(message += " (Unknown level)");
                break;
        }
    }
}