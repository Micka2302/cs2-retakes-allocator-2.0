using System.Text.Json;

namespace KitsuneMenu.Core;

public class MenuTranslations
{
    private const string TRANSLATIONS_FILE = "kitsune_menu_translations.jsonc";
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
    };

    // Menu UI elements
    public string ItemsCounter { get; set; } = "Items {0}/{1}";

    // Footer elements
    public string FooterSelect { get; set; } = "Select";
    public string FooterBack { get; set; } = "Back";
    public string FooterExit { get; set; } = "Exit";
    public string FooterSeparator { get; set; } = " | ";

    // Button display names
    public Dictionary<string, string> ButtonNames { get; set; } = new()
    {
        ["Jump"] = "Jump",
        ["Use"] = "Use",
        ["Speed"] = "Speed",
        ["Scoreboard"] = "TAB",
        ["Duck"] = "CTRL",
        ["Attack"] = "Mouse1",
        ["Attack2"] = "Mouse2",
        ["Reload"] = "R",
        ["Walk"] = "Shift"
    };

    private static MenuTranslations? _instance;
    public static MenuTranslations Instance => _instance ??= Load();

    public static MenuTranslations Load()
    {
        var translationsPath = MenuFileSystem.Combine(TRANSLATIONS_FILE);
        MenuFileSystem.EnsureDirectoryForFile(translationsPath);
        var translations = new MenuTranslations();

        if (File.Exists(translationsPath))
        {
            try
            {
                var jsonContent = File.ReadAllText(translationsPath);
                var loaded = JsonSerializer.Deserialize<MenuTranslations>(jsonContent, _jsonOptions);
                if (loaded != null) translations = loaded;
            }
            catch
            {
                // Use defaults if load fails
            }
        }
        else
        {
            Save();
        }

        return translations;
    }

    private static void Save()
    {
        var translationsPath = MenuFileSystem.Combine(TRANSLATIONS_FILE);
        var translationsContent = @"{
    /* Menu UI text translations */

    // Counter shown when menu has more items than can be displayed
    ""ItemsCounter"": ""Items {0}/{1}"",

    // Footer action labels
    ""FooterSelect"": ""Select"",
    ""FooterBack"": ""Back"",
    ""FooterExit"": ""Exit"",
    ""FooterSeparator"": "" | "",

    // Button display names (used in footer)
    ""ButtonNames"": {
        ""Jump"": ""Jump"",
        ""Use"": ""Use"",
        ""Speed"": ""Speed"",
        ""Scoreboard"": ""TAB"",
        ""Duck"": ""CTRL"",
        ""Attack"": ""Mouse1"",
        ""Attack2"": ""Mouse2"",
        ""Reload"": ""R"",
        ""Walk"": ""Shift""
    }
}";

        try
        {
            MenuFileSystem.EnsureDirectoryForFile(translationsPath);
            File.WriteAllText(translationsPath, translationsContent);
        }
        catch
        {
            // Ignore save errors
        }
    }

    public string GetButtonDisplay(string buttonName)
    {
        return ButtonNames.TryGetValue(buttonName, out var display) ? display : buttonName;
    }

    public string GetButtonsDisplay(List<string> buttonNames)
    {
        if (buttonNames.Count == 0) return "None";
        if (buttonNames.Count == 1) return GetButtonDisplay(buttonNames[0]);

        var displays = buttonNames.Select(b => GetButtonDisplay(b));
        return string.Join("/", displays);
    }
}
