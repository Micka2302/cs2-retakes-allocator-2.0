using CounterStrikeSharp.API.Core;
using KitsuneMenu.Core.Interfaces;
using KitsuneMenu.Core.Enums;

namespace KitsuneMenu.Core.MenuItems;

public class ToggleMenuItem(string text, bool defaultValue = false, Action<CCSPlayerController, bool>? onToggle = null, MenuTextSize size = MenuTextSize.Medium) : IMenuItem
{
    public string Text { get; set; } = text;
    public bool Value { get; set; } = defaultValue;
    public Action<CCSPlayerController, bool>? OnToggle { get; set; } = onToggle;
    public Func<CCSPlayerController, bool>? VisibilityCheck { get; set; }
    public Func<CCSPlayerController, bool>? EnabledCheck { get; set; }
    public Func<CCSPlayerController, bool>? ValidationCheck { get; set; }
    public Action<CCSPlayerController>? OnValidationFailed { get; set; }
    public MenuTextSize Size { get; set; } = size;
    public bool CloseOnSelect { get; set; }

    public bool Visible => true;
    public bool Enabled => true;

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

        var status = Value ? "<font color='#008000'>✔</font>" : "<font color='#FF0000'>✘</font>";

        if (!CanInteract(player))
        {
            return $"<font class='{sizeClass}' color='#8f3b3b'>{Text}: {status}</font>";
        }

        return $"<font class='{sizeClass}'>{Text}: {status}</font>";
    }
    
    public MenuTextSize GetTextSize()
    {
        return Size;
    }

    public void Toggle(CCSPlayerController player)
    {
        if (!CanInteract(player)) return;
        
        if (ValidationCheck != null && !ValidationCheck(player))
        {
            OnValidationFailed?.Invoke(player);
            return;
        }
        
        Value = !Value;
        OnToggle?.Invoke(player, Value);
    }
}