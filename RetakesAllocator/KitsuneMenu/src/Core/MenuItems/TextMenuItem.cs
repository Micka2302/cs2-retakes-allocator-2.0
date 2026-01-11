using CounterStrikeSharp.API.Core;
using KitsuneMenu.Core.Interfaces;
using KitsuneMenu.Core.Enums;

namespace KitsuneMenu.Core.MenuItems;

public class TextMenuItem : IMenuItem
{
    public string Text { get; set; }
    public TextAlign Alignment { get; set; }
    public MenuTextSize Size { get; set; }
    public Func<CCSPlayerController, bool>? VisibilityCheck { get; set; }
    public Func<string>? DynamicText { get; set; }

    public bool Visible => true;
    public bool Enabled => false;

    public TextMenuItem(string text, TextAlign alignment = TextAlign.Left, MenuTextSize size = MenuTextSize.Medium)
    {
        Text = text;
        Alignment = alignment;
        Size = size;
    }

    public TextMenuItem(Func<string> dynamicText, TextAlign alignment = TextAlign.Left, MenuTextSize size = MenuTextSize.Medium)
    {
        Text = "";
        DynamicText = dynamicText;
        Alignment = alignment;
        Size = size;
    }

    public bool ShouldShow(CCSPlayerController player)
    {
        return VisibilityCheck?.Invoke(player) ?? true;
    }

    public bool CanInteract(CCSPlayerController player)
    {
        return false;
    }

    public string GetDisplayText(CCSPlayerController player)
    {
        var text = DynamicText?.Invoke() ?? Text;
        
        // Get size class
        var sizeClass = Utils.MenuSizeHelper.GetCssClass(Size);

        // Apply size
        text = $"<font class='{sizeClass}'>{text}</font>";

        return Alignment switch
        {
            TextAlign.Center => $"<center>{text}</center>",
            TextAlign.Right => $"<div align='right'>{text}</div>",
            _ => text
        };
    }
    
    public MenuTextSize GetTextSize()
    {
        return Size;
    }
}

public enum TextAlign
{
    Left,
    Center,
    Right
}