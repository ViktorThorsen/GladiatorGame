using System.Collections.Generic;

public static class ProfileImageMapper
{
    private static readonly Dictionary<string, string> hairMap = new()
    {
        { "green", "brownHair" },
        { "blue", "blueHair" },
        { "purple", "blackHair" },
        { "red", "redHair" },
        {"black", "blondeHair"}
    };

    private static readonly Dictionary<string, string> eyesMap = new()
    {
        { "blue", "blueEyes" },
        { "green", "greenEyes" },
        { "default", "brownEyes" }
    };

    private static readonly Dictionary<string, string> chestMap = new()
    {
        { "blue", "blueBody" },
        { "green", "greenBody" },
        { "red", "redBody" },
        { "default", "purpleBody" }
    };

    public static string MapHair(string label)
    {
        if (string.IsNullOrEmpty(label)) return "brownHair";
        return hairMap.TryGetValue(label, out var result) ? result : "brownHair";
    }

    public static string MapEyes(string label)
    {
        if (string.IsNullOrEmpty(label)) return eyesMap["default"];
        return eyesMap.TryGetValue(label, out var result) ? result : eyesMap["default"];
    }

    public static string MapChest(string label)
    {
        if (string.IsNullOrEmpty(label)) return chestMap["default"];
        return chestMap.TryGetValue(label, out var result) ? result : chestMap["default"];
    }
}