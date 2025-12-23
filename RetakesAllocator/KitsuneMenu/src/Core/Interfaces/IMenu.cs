using CounterStrikeSharp.API.Core;

namespace KitsuneMenu.Core.Interfaces;

public interface IMenu
{
    string Title { get; set; }
    List<IMenuItem> Items { get; }
    IMenu? Parent { get; set; }
    TimeSpan? AutoCloseTime { get; set; }
    MenuButtonOverrides? ButtonOverrides { get; set; }
    int MaxVisibleItems { get; set; }
    bool? FreezeOverride { get; set; }

    event Action<CCSPlayerController>? OnOpen;
    event Action<CCSPlayerController>? OnClose;
    event Action<CCSPlayerController>? OnBack;
    event Action<CCSPlayerController, IMenuItem>? OnItemSelected;
    event Action<CCSPlayerController, IMenuItem>? OnItemHovered;
    event Action<CCSPlayerController>? BeforeRender;
    event Action<CCSPlayerController>? AfterRender;

    void Show(CCSPlayerController player);
    void Close(CCSPlayerController player);
    void Back(CCSPlayerController player);
}