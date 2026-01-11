using CounterStrikeSharp.API.Core;
using KitsuneMenu.Core.Enums;

namespace KitsuneMenu.Core.Interfaces;

public interface IMenuItem
{
    string Text { get; set; }
    bool Visible { get; }
    bool Enabled { get; }
    
    bool ShouldShow(CCSPlayerController player);
    bool CanInteract(CCSPlayerController player);
    string GetDisplayText(CCSPlayerController player);
    MenuTextSize GetTextSize();
}