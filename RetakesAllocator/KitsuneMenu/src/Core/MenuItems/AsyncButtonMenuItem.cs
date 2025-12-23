using CounterStrikeSharp.API.Core;
using KitsuneMenu.Core.Interfaces;
using KitsuneMenu.Core.Enums;

namespace KitsuneMenu.Core.MenuItems;

public class AsyncButtonMenuItem(string text, Func<CCSPlayerController, Task>? onClickAsync = null, MenuTextSize size = MenuTextSize.Medium) : IMenuItem
{
    public string Text { get; set; } = text;
    public Func<CCSPlayerController, Task>? OnClickAsync { get; set; } = onClickAsync;
    public Func<CCSPlayerController, bool>? VisibilityCheck { get; set; }
    public Func<CCSPlayerController, bool>? EnabledCheck { get; set; }
    public Func<CCSPlayerController, bool>? ValidationCheck { get; set; }
    public Action<CCSPlayerController>? OnValidationFailed { get; set; }
    public MenuTextSize Size { get; set; } = size;
    public bool CloseOnSelect { get; set; }

    public bool Visible => true;
    public bool Enabled => true;
    public bool IsLoading { get; set; }

    private string? _loadingText;

    public bool ShouldShow(CCSPlayerController player)
    {
        return VisibilityCheck?.Invoke(player) ?? true;
    }

    public bool CanInteract(CCSPlayerController player)
    {
        return !IsLoading && (EnabledCheck?.Invoke(player) ?? true);
    }

    public string GetDisplayText(CCSPlayerController player)
    {
        var sizeClass = Utils.MenuSizeHelper.GetCssClass(Size);

        if (IsLoading)
        {
            return $"<font class='{sizeClass}' color='#ffaa00'>{_loadingText ?? "Loading..."}</font>";
        }

        if (!CanInteract(player))
        {
            return $"<font class='{sizeClass}' color='#8f3b3b'>{Text}</font>";
        }

        return $"<font class='{sizeClass}'>{Text}</font>";
    }
    
    public MenuTextSize GetTextSize()
    {
        return Size;
    }

    public async Task ExecuteAsync(CCSPlayerController player, string? loadingText = null)
    {
        if (OnClickAsync == null) return;

        _loadingText = loadingText;

        try
        {
            await OnClickAsync.Invoke(player);
        }
        finally
        {
            _loadingText = null;
        }
    }
    
    public void SetLoadingText(string? text)
    {
        _loadingText = text;
    }
}