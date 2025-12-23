using CounterStrikeSharp.API.Core;
using KitsuneMenu.Core.Interfaces;
using KitsuneMenu.Core.Enums;

namespace KitsuneMenu.Core.MenuItems;

public class SubmenuMenuItem : IMenuItem
{
    public string Text { get; set; }
    public IMenu? Submenu { get; set; }
    public Func<IMenu>? SubmenuBuilder { get; set; }
    public Func<CCSPlayerController, bool>? VisibilityCheck { get; set; }
    public Func<CCSPlayerController, bool>? EnabledCheck { get; set; }
    public MenuTextSize Size { get; set; }

    public bool Visible => true;
    public bool Enabled => true;

    public SubmenuMenuItem(string text, IMenu? submenu = null, MenuTextSize size = MenuTextSize.Medium)
    {
        Text = text;
        Submenu = submenu;
        Size = size;
    }

    public SubmenuMenuItem(string text, Func<IMenu> submenuBuilder, MenuTextSize size = MenuTextSize.Medium)
    {
        Text = text;
        SubmenuBuilder = submenuBuilder;
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

        var arrow = $" <font color='#ff3333' class='{sizeClass}'>▶</font>";

        if (!CanInteract(player))
        {
            return $"<font class='{sizeClass}' color='#8f3b3b'>{Text}{arrow}</font>";
        }

        return $"<font class='{sizeClass}'>{Text}{arrow}</font>";
    }
    
    public MenuTextSize GetTextSize()
    {
        return Size;
    }

    public IMenu? GetSubmenu()
    {
        return Submenu ?? SubmenuBuilder?.Invoke();
    }
}