using KitsuneMenu.Core.Enums;

namespace KitsuneMenu.Core.Utils;

public static class MenuSizeHelper
{
    public static string GetCssClass(MenuTextSize size)
    {
        return size switch
        {
            MenuTextSize.ExtraSmall => "fontSize-xs",
            MenuTextSize.Small => "fontSize-s",
            MenuTextSize.SmallMedium => "fontSize-sm",
            MenuTextSize.Medium => "fontSize-m",
            MenuTextSize.MediumLarge => "fontSize-ml",
            MenuTextSize.Large => "fontSize-l",
            MenuTextSize.ExtraLarge => "fontSize-xl",
            _ => "fontSize-m"
        };
    }
}