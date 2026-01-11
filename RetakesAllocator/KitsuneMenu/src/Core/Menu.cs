using CounterStrikeSharp.API.Core;
using KitsuneMenu.Core.Interfaces;

namespace KitsuneMenu.Core;

public class Menu : IMenu
{
    public string Title { get; set; }
    public List<IMenuItem> Items { get; } = new();
    public IMenu? Parent { get; set; }
    public TimeSpan? AutoCloseTime { get; set; }
    public MenuButtonOverrides? ButtonOverrides { get; set; }
    public int MaxVisibleItems { get; set; } = 5;
    public bool? FreezeOverride { get; set; }

    public event Action<CCSPlayerController>? OnOpen;
    public event Action<CCSPlayerController>? OnClose;
    public event Action<CCSPlayerController>? OnBack;
    public event Action<CCSPlayerController, IMenuItem>? OnItemSelected;
    public event Action<CCSPlayerController, IMenuItem>? OnItemHovered;
    public event Action<CCSPlayerController>? BeforeRender;
    public event Action<CCSPlayerController>? AfterRender;

    public Menu(string title)
    {
        Title = title;
    }

    public void Show(CCSPlayerController player)
    {
        KitsuneMenu.ShowMenu(player, this);
        OnOpen?.Invoke(player);
    }

    public void Close(CCSPlayerController player)
    {
        KitsuneMenu.CloseMenu(player);
        OnClose?.Invoke(player);
    }

    public void Back(CCSPlayerController player)
    {
        if (Parent != null)
        {
            Parent.Show(player);
            OnBack?.Invoke(player);
        }
        else
        {
            Close(player);
        }
    }

    internal void TriggerItemSelected(CCSPlayerController player, IMenuItem item)
    {
        OnItemSelected?.Invoke(player, item);
    }

    internal void TriggerItemHovered(CCSPlayerController player, IMenuItem item)
    {
        OnItemHovered?.Invoke(player, item);
    }

    internal void TriggerBeforeRender(CCSPlayerController player)
    {
        BeforeRender?.Invoke(player);
    }

    internal void TriggerAfterRender(CCSPlayerController player)
    {
        AfterRender?.Invoke(player);
    }

    internal void TriggerBack(CCSPlayerController player)
    {
        OnBack?.Invoke(player);
    }
}