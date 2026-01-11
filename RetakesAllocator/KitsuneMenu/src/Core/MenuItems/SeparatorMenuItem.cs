using CounterStrikeSharp.API.Core;
using KitsuneMenu.Core.Interfaces;
using KitsuneMenu.Core.Enums;

namespace KitsuneMenu.Core.MenuItems;

public class SeparatorMenuItem : IMenuItem
{
    public string Text { get; set; }
    public bool Visible => true;
    public bool Enabled => false;

    public SeparatorMenuItem()
    {
        Text = "─────────────────────";
    }

    public bool ShouldShow(CCSPlayerController player) => true;
    public bool CanInteract(CCSPlayerController player) => false;

    public string GetDisplayText(CCSPlayerController player)
    {
        return $"<font color='#444444'>{Text}</font>";
    }
    
    public MenuTextSize GetTextSize()
    {
        return MenuTextSize.Small; // Separators are typically small
    }
}