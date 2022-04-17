using System;
using System.Text;
using UnityEngine;

[Serializable]
public struct LogString
{
    private StringBuilder stringBuilder;
    
    public LogString(string message)
    {
        stringBuilder = new StringBuilder();
        stringBuilder.Append("<color=");
        stringBuilder.Append(Color.black);
        stringBuilder.Append(">");
        stringBuilder.Append(message);
        stringBuilder.Append("</color>");
    }
    
    public LogString(Color color, string message)
    {
        //GlobalPrefixColor = Color32ToRGBString(isDarkMode ? new Color32(115, 172, 229, 255) : new Color32(20, 64, 120, 255));
        stringBuilder = new StringBuilder();
        stringBuilder.Append("<color=");
        stringBuilder.Append(Color32ToRGBString(color));
        stringBuilder.Append(">");
        stringBuilder.Append(message);
        stringBuilder.Append("</color>");
    }
    
    private static string ColorToRGBString(Color c) 
    {
        return $"#{Color32ToRGB24(c):X6}";
    }
    
    private static int Color32ToRGB24(Color32 c) {
        return (c.r << 16) | (c.g << 8) | c.b;
    }

    private static string Color32ToRGBString(Color32 c) 
    {
        return $"#{Color32ToRGB24(c):X6}";
    }
    public override string ToString()
    {
        return stringBuilder.ToString();
    }
}