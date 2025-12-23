using CounterStrikeSharp.API.Core;
using KitsuneMenu.Core.Interfaces;
using KitsuneMenu.Core.Enums;

namespace KitsuneMenu.Core.MenuItems;

public class SliderMenuItem(string text, float min = 0, float max = 10, float defaultValue = 5, float step = 1, Action<CCSPlayerController, float>? onChange = null, MenuTextSize size = MenuTextSize.Medium) : IMenuItem
{
    public string Text { get; set; } = text;
    public float Value { get; set; } = Math.Clamp(defaultValue, min, max);
    public float Min { get; set; } = min;
    public float Max { get; set; } = max;
    public float Step { get; set; } = step;
    public Action<CCSPlayerController, float>? OnChange { get; set; } = onChange;
    public Func<CCSPlayerController, bool>? VisibilityCheck { get; set; }
    public Func<CCSPlayerController, bool>? EnabledCheck { get; set; }
    public MenuTextSize Size { get; set; } = size;

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

        int totalBars = 10;
        float percentage = (Value - Min) / (Max - Min);
        int filledBars = (int)(percentage * totalBars);

        string slider = "<font color='#ff3333'>(</font>";
        for (int i = 0; i < totalBars; i++)
        {
            if (i < filledBars)
                slider += "<font color='#ff3333'>■</font>";
            else
                slider += "<font color='#666666'>□</font>";
        }
        slider += $"<font color='#ff3333'>)</font> {Value:F1}";

        if (!CanInteract(player))
        {
            return $"<font class='{sizeClass}' color='#8f3b3b'>{Text}: {slider}</font>";
        }

        return $"<font class='{sizeClass}'>{Text}: {slider}</font>";
    }
    
    public MenuTextSize GetTextSize()
    {
        return Size;
    }

    public void Increase(CCSPlayerController player)
    {
        if (!CanInteract(player)) return;
        var newValue = Math.Min(Value + Step, Max);
        if (newValue != Value)
        {
            Value = newValue;
            OnChange?.Invoke(player, Value);
        }
    }

    public void Decrease(CCSPlayerController player)
    {
        if (!CanInteract(player)) return;
        var newValue = Math.Max(Value - Step, Min);
        if (newValue != Value)
        {
            Value = newValue;
            OnChange?.Invoke(player, Value);
        }
    }
}