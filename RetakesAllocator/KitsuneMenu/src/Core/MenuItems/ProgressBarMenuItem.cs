using CounterStrikeSharp.API.Core;
using KitsuneMenu.Core.Interfaces;
using KitsuneMenu.Core.Enums;

namespace KitsuneMenu.Core.MenuItems;

public class ProgressBarMenuItem(string text, Func<float> progressProvider, int barWidth = 20, MenuTextSize size = MenuTextSize.Medium) : IMenuItem
{
    public string Text { get; set; } = text;
    public Func<float> ProgressProvider { get; set; } = progressProvider;
    public int BarWidth { get; set; } = barWidth;
    public string FilledChar { get; set; } = "█";
    public string EmptyChar { get; set; } = "░";
    public bool ShowPercentage { get; set; } = true;
    public MenuTextSize Size { get; set; } = size;

    public bool Visible => true;
    public bool Enabled => false;

    public bool ShouldShow(CCSPlayerController player) => true;
    public bool CanInteract(CCSPlayerController player) => false;

    public string GetDisplayText(CCSPlayerController player)
    {
        var sizeClass = Utils.MenuSizeHelper.GetCssClass(Size);

        var progress = Math.Clamp(ProgressProvider(), 0f, 1f);
        var filledCount = (int)(progress * BarWidth);
        var emptyCount = BarWidth - filledCount;

        var bar = "";
        for (int i = 0; i < filledCount; i++)
            bar += $"<font color='#ff3333'>{FilledChar}</font>";
        for (int i = 0; i < emptyCount; i++)
            bar += $"<font color='#666666'>{EmptyChar}</font>";

        var percentage = ShowPercentage ? $" {(int)(progress * 100)}%" : "";

        return $"<font class='{sizeClass}'>{Text}: {bar}{percentage}</font>";
    }
    
    public MenuTextSize GetTextSize()
    {
        return Size;
    }
}