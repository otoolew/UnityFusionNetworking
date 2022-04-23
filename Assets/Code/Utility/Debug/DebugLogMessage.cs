using UnityEngine;

public static class DebugLogMessage
{
    public static void Log(string message)
    {
        Debug.Log(new LogString(message.ToString()));
    }
    public static void Log(Color color, string message)
    {
        Debug.Log(new LogString(color,message.ToString()));
    }
}

