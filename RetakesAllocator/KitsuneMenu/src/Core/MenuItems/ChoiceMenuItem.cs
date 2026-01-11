using CounterStrikeSharp.API.Core;
using KitsuneMenu.Core.Interfaces;
using KitsuneMenu.Core.Enums;

namespace KitsuneMenu.Core.MenuItems;

public class ChoiceMenuItem : IMenuItem
{
    public string Text { get; set; }
    public List<string> Choices { get; set; }
    public int SelectedIndex { get; set; }
    public Action<CCSPlayerController, string>? OnChange { get; set; }
    public Func<CCSPlayerController, bool>? VisibilityCheck { get; set; }
    public Func<CCSPlayerController, bool>? EnabledCheck { get; set; }
    public MenuTextSize Size { get; set; }

    public bool Visible => true;
    public bool Enabled => true;

    public string SelectedChoice => Choices.Count > 0 ? Choices[SelectedIndex] : "";

    public ChoiceMenuItem(string text, IEnumerable<string> choices, string? defaultChoice = null, Action<CCSPlayerController, string>? onChange = null, MenuTextSize size = MenuTextSize.Medium)
    {
        Text = text;
        Choices = [.. choices];
        SelectedIndex = 0;
        Size = size;

        if (defaultChoice != null)
        {
            var index = Choices.IndexOf(defaultChoice);
            if (index >= 0) SelectedIndex = index;
        }

        OnChange = onChange;
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

        var choice = $"<font color='#ff3333'>[</font>{SelectedChoice}<font color='#ff3333'>]</font>";

        if (!CanInteract(player))
        {
            return $"<font class='{sizeClass}' color='#8f3b3b'>{Text}: {choice}</font>";
        }
        return $"<font class='{sizeClass}'>{Text}: {choice}</font>";
    }
    
    public MenuTextSize GetTextSize()
    {
        return Size;
    }

    public void Next(CCSPlayerController player)
    {
        if (!CanInteract(player) || Choices.Count == 0) return;
        SelectedIndex = (SelectedIndex + 1) % Choices.Count;
        OnChange?.Invoke(player, SelectedChoice);
    }

    public void Previous(CCSPlayerController player)
    {
        if (!CanInteract(player) || Choices.Count == 0) return;
        SelectedIndex = (SelectedIndex - 1 + Choices.Count) % Choices.Count;
        OnChange?.Invoke(player, SelectedChoice);
    }
}