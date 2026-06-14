// Stolen from https://github.com/B3none/cs2-retakes/blob/014663222fa95bb9f506284814ae62205630416c/Modules/Translator.cs

using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;

namespace RetakesAllocatorCore;

public class Translator
{
    private static Translator? _instance;

    public static Translator Initialize(IStringLocalizer localizer)
    {
        _instance = new(localizer);
        return _instance;
    }

    public static bool IsInitialized => _instance is not null;

    public static Translator Instance => _instance ?? throw new Exception("Translator is not initialized.");
    
    private IStringLocalizer _stringLocalizerImplementation;

    public Translator(IStringLocalizer localizer)
    {
        _stringLocalizerImplementation = localizer;
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return _stringLocalizerImplementation.GetAllStrings(includeParentCultures);
    }

    public string this[string name] => Translate(name);

    public string this[string name, params object[] arguments] => Translate(name, arguments);

    public string Raw(string key, params object[] arguments)
    {
        var localizedString = _stringLocalizerImplementation[key];

        if (localizedString == null || localizedString.ResourceNotFound)
        {
            return key;
        }

        return FormatArguments(localizedString.Value, arguments);
    }

    private string Translate(string key, params object[] arguments)
    {
        var isCenter = key.StartsWith("center.");
        var localizedString = _stringLocalizerImplementation[key];

        if ((localizedString == null || localizedString.ResourceNotFound) && isCenter)
        {
            localizedString = _stringLocalizerImplementation[key["center.".Length..]];
        }

        if (localizedString == null || localizedString.ResourceNotFound)
        {
            return key;
        }

        var value = FormatArguments(localizedString.Value, arguments);

        return isCenter ? value : Color(value);
    }

    private static string FormatArguments(string text, object[] arguments)
    {
        for (var i = 0; i < arguments.Length; i++)
        {
            text = text.Replace($"{{{i}}}", arguments[i]?.ToString() ?? string.Empty);
        }

        return text;
    }

    public static string Color(string text)
    {
        text = text
            .Replace("{teamcolor}CT", $"{ChatColors.Blue}CT", StringComparison.OrdinalIgnoreCase)
            .Replace("{teamcolors}CT", $"{ChatColors.Blue}CT", StringComparison.OrdinalIgnoreCase)
            .Replace("{teamcolor}T", $"{ChatColors.Orange}T", StringComparison.OrdinalIgnoreCase)
            .Replace("{teamcolors}T", $"{ChatColors.Orange}T", StringComparison.OrdinalIgnoreCase);

        var colorTags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["green"] = ChatColors.Green.ToString(),
            ["red"] = ChatColors.Red.ToString(),
            ["yellow"] = ChatColors.Yellow.ToString(),
            ["blue"] = ChatColors.Blue.ToString(),
            ["purple"] = ChatColors.Purple.ToString(),
            ["orange"] = ChatColors.Orange.ToString(),
            ["white"] = ChatColors.White.ToString(),
            ["normal"] = ChatColors.White.ToString(),
            ["grey"] = ChatColors.Grey.ToString(),
            ["gray"] = ChatColors.Grey.ToString(),
            ["lightred"] = ChatColors.LightRed.ToString(),
            ["light_red"] = ChatColors.LightRed.ToString(),
            ["lightblue"] = ChatColors.LightBlue.ToString(),
            ["light_blue"] = ChatColors.LightBlue.ToString(),
            ["lightpurple"] = ChatColors.LightPurple.ToString(),
            ["light_purple"] = ChatColors.LightPurple.ToString(),
            ["lightyellow"] = ChatColors.LightYellow.ToString(),
            ["light_yellow"] = ChatColors.LightYellow.ToString(),
            ["darkred"] = ChatColors.DarkRed.ToString(),
            ["dark_red"] = ChatColors.DarkRed.ToString(),
            ["darkblue"] = ChatColors.DarkBlue.ToString(),
            ["dark_blue"] = ChatColors.DarkBlue.ToString(),
            ["bluegrey"] = ChatColors.BlueGrey.ToString(),
            ["blue_grey"] = ChatColors.BlueGrey.ToString(),
            ["olive"] = ChatColors.Olive.ToString(),
            ["lime"] = ChatColors.Lime.ToString(),
            ["gold"] = ChatColors.Gold.ToString(),
            ["silver"] = ChatColors.Silver.ToString(),
            ["magenta"] = ChatColors.Magenta.ToString(),
        };

        foreach (var (tag, color) in colorTags)
        {
            text = text
                .Replace($"{{{tag}}}", color, StringComparison.OrdinalIgnoreCase)
                .Replace($"[{tag}]", color, StringComparison.OrdinalIgnoreCase);
        }

        return text;
    }
}
