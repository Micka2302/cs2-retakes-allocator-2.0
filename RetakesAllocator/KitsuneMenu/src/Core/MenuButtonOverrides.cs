using CounterStrikeSharp.API;

namespace KitsuneMenu.Core;

public class MenuButtonOverrides
{
    public PlayerButtons? Select { get; set; }
    public PlayerButtons? Back { get; set; }
    public PlayerButtons? Exit { get; set; }

    // Helper methods to parse button names
    public static PlayerButtons ParseButton(string buttonName)
    {
        switch (buttonName.ToLower())
        {
            case "scoreboard":
                return (PlayerButtons)(1UL << 33);
            case "inspect":
                return (PlayerButtons)(1UL << 35);
        }

        if (Enum.TryParse<PlayerButtons>(buttonName, true, out var button))
        {
            return button;
        }

        Console.WriteLine($"Warning: Invalid button name '{buttonName}', falling back to 0");
        return 0;
    }

    public static PlayerButtons ParseButtons(params string[] buttonNames)
    {
        PlayerButtons result = 0;
        foreach (var buttonName in buttonNames)
        {
            // Skip WASD buttons - they are reserved for navigation
            if (IsWASDButton(buttonName))
            {
                Console.WriteLine($"Warning: '{buttonName}' is reserved for navigation and cannot be used for other actions");
                continue;
            }
            result |= ParseButton(buttonName);
        }
        return result;
    }

    private static bool IsWASDButton(string buttonName)
    {
        var lower = buttonName.ToLower();
        return lower == "forward" || lower == "back" || lower == "moveleft" || lower == "moveright" ||
               lower == "w" || lower == "a" || lower == "s" || lower == "d";
    }
}