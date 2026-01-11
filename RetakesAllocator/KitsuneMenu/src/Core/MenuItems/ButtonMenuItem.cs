using CounterStrikeSharp.API.Core;
using KitsuneMenu.Core.Interfaces;
using KitsuneMenu.Core.Enums;

namespace KitsuneMenu.Core.MenuItems;

public class ButtonMenuItem : IMenuItem
{
    public string Text { get; set; }
    public Action<CCSPlayerController>? OnClick { get; set; }
    public Func<CCSPlayerController, bool>? VisibilityCheck { get; set; }
    public Func<CCSPlayerController, bool>? EnabledCheck { get; set; }
    public Func<CCSPlayerController, bool>? ValidationCheck { get; set; }
    public Action<CCSPlayerController>? OnValidationFailed { get; set; }
    public MenuTextSize Size { get; set; }
    public bool CloseOnSelect { get; set; }

    public bool Visible => true;
    public bool Enabled => true;

    public ButtonMenuItem(string text, Action<CCSPlayerController>? onClick = null, MenuTextSize size = MenuTextSize.Medium)
    {
        Text = text;
        OnClick = onClick;
        Size = size;
    }

    public bool ShouldShow(CCSPlayerController player)
    {
        return VisibilityCheck?.Invoke(player) ?? true;
    }

    public bool CanInteract(CCSPlayerController player)
    {
        return EnabledCheck?.Invoke(player) ?? true;
    }

    public string GetDisplayText(CCSPlayerController player)
    {
        var sizeClass = Utils.MenuSizeHelper.GetCssClass(Size);

        var displayText = $"<font class='{sizeClass}'>{Text}</font>";

        if (!CanInteract(player))
        {
            return $"<font class='{sizeClass}' color='#8f3b3b'>{Text}</font>";
        }

        return displayText;
    }
    
    public MenuTextSize GetTextSize()
    {
        return Size;
    }
}