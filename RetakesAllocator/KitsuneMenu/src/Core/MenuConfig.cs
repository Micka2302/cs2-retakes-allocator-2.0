using CounterStrikeSharp.API;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KitsuneMenu.Core;

public class MenuConfig
{
    private const string CONFIG_FILE = "kitsune_menu_config.jsonc";
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
    };

    public List<string> Select { get; set; } = new() { "Jump", "Use" };
    public List<string> Back { get; set; } = new() { "Speed" };
    public List<string> Exit { get; set; } = new() { "Scoreboard" };
    public bool FreezePlayer { get; set; } = true;

    [JsonIgnore]
    public PlayerButtons SelectButtons { get; private set; }
    [JsonIgnore]
    public PlayerButtons BackButtons { get; private set; }
    [JsonIgnore]
    public PlayerButtons ExitButtons { get; private set; }

    public static MenuConfig Load()
    {
        var configPath = MenuFileSystem.Combine(CONFIG_FILE);
        MenuFileSystem.EnsureDirectoryForFile(configPath);
        var config = new MenuConfig();

        if (File.Exists(configPath))
        {
            try
            {
                var jsonContent = File.ReadAllText(configPath);
                var loaded = JsonSerializer.Deserialize<MenuConfig>(jsonContent, _jsonOptions);
                if (loaded != null) config = loaded;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading menu configuration: {ex.Message}");
            }
        }
        else
        {
            Save();
        }

        config.ParseButtons();
        return config;
    }

    private static void Save()
    {
        var configPath = MenuFileSystem.Combine(CONFIG_FILE);
        MenuFileSystem.EnsureDirectoryForFile(configPath);
        var configContent = @"{
    /* Configuration for KitsuneMenu

    Available buttons:
        Attack      - Primary attack button
        Jump        - Jump
        Duck        - Crouch
        Forward     - Move forward
        Back        - Move backward
        Use         - Use key
        Cancel      - Cancel action
        Left        - Turn left
        Right       - Turn right
        Moveleft    - Strafe left
        Moveright   - Strafe right
        Attack2     - Secondary attack
        Run         - Run
        Reload      - Reload weapon
        Alt1        - Alternative button 1
        Alt2        - Alternative button 2
        Speed       - Sprint/Fast movement
        Walk        - Walk
        Zoom        - Zoom view
        Weapon1     - Primary weapon
        Weapon2     - Secondary weapon
        Bullrush    - Bullrush
        Grenade1    - First grenade
        Grenade2    - Second grenade
        Attack3     - Third attack
        Scoreboard  - Show scoreboard (TAB)
        Inspect     - Inspect weapon (F)
    */

    // Buttons to select menu items (multiple buttons supported)
    ""Select"": [""Jump"", ""Use""],

    // Buttons to go back in submenus
    ""Back"": [""Speed""],

    // Exit menu buttons
    ""Exit"": [""Scoreboard""],

    // Whether to freeze player when menu is open
    ""FreezePlayer"": true
}";

        try
        {
            File.WriteAllText(configPath, configContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating default configuration: {ex.Message}");
        }
    }

    private void ParseButtons()
    {
        SelectButtons = ParseButtonsByNames(Select);
        BackButtons = ParseButtonsByNames(Back);
        ExitButtons = ParseButtonsByNames(Exit);
    }

    private static PlayerButtons ParseButtonsByNames(List<string> buttonNames)
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
            result |= ParseButtonByName(buttonName);
        }

        return result;
    }

    private static bool IsWASDButton(string buttonName)
    {
        var lower = buttonName.ToLower();
        return lower == "forward" || lower == "back" || lower == "moveleft" || lower == "moveright" ||
               lower == "w" || lower == "a" || lower == "s" || lower == "d";
    }

    private static PlayerButtons ParseButtonByName(string buttonName)
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
}
