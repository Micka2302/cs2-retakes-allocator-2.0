using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using KitsuneMenu.Core.Interfaces;
using KitsuneMenu.Core.MenuItems;
using KitsuneMenu.Core.Enums;

namespace KitsuneMenu.Core;

public class MenuBuilder
{
    private readonly string _title;
    private readonly List<IMenuItem> _items = new();
    private IMenu? _parent;
    private TimeSpan? _autoCloseTime;
    private MenuButtonOverrides? _buttonOverrides;
    private int _maxVisibleItems = 5;
    private bool? _freezeOverride;

    public MenuBuilder(string title)
    {
        _title = title;
    }

    public MenuBuilder AddButton(string text, Action<CCSPlayerController>? onClick = null, MenuTextSize size = MenuTextSize.Medium)
    {
        _items.Add(new ButtonMenuItem(text, onClick, size));
        return this;
    }

    public MenuBuilder AddButton(string text, Action<CCSPlayerController>? onClick)
    {
        return AddButton(text, onClick, MenuTextSize.Medium);
    }

    public MenuBuilder AddToggle(string text, bool defaultValue = false, Action<CCSPlayerController, bool>? onToggle = null, MenuTextSize size = MenuTextSize.Medium)
    {
        _items.Add(new ToggleMenuItem(text, defaultValue, onToggle, size));
        return this;
    }

    public MenuBuilder AddToggle(string text, bool defaultValue, Action<CCSPlayerController, bool>? onToggle)
    {
        return AddToggle(text, defaultValue, onToggle, MenuTextSize.Medium);
    }

    public MenuBuilder AddSlider(string text, float min, float max, float defaultValue, float step = 1f, Action<CCSPlayerController, float>? onChange = null, MenuTextSize size = MenuTextSize.Medium)
    {
        _items.Add(new SliderMenuItem(text, min, max, defaultValue, step, onChange, size));
        return this;
    }

    public MenuBuilder AddSlider(string text, float min, float max, float defaultValue, float step, Action<CCSPlayerController, float>? onChange)
    {
        return AddSlider(text, min, max, defaultValue, step, onChange, MenuTextSize.Medium);
    }

    public MenuBuilder AddChoice(string text, string[] choices, string? defaultChoice = null, Action<CCSPlayerController, string>? onChange = null, MenuTextSize size = MenuTextSize.Medium)
    {
        _items.Add(new ChoiceMenuItem(text, choices, defaultChoice, onChange, size));
        return this;
    }

    public MenuBuilder AddChoice(string text, string[] choices, string? defaultChoice, Action<CCSPlayerController, string>? onChange)
    {
        return AddChoice(text, choices, defaultChoice, onChange, MenuTextSize.Medium);
    }

    public MenuBuilder AddSubmenu(string text, IMenu submenu, MenuTextSize size = MenuTextSize.Medium)
    {
        _items.Add(new SubmenuMenuItem(text, submenu, size));
        return this;
    }

    public MenuBuilder AddSubmenu(string text, IMenu submenu)
    {
        return AddSubmenu(text, submenu, MenuTextSize.Medium);
    }

    public MenuBuilder AddSubmenu(string text, Func<IMenu> submenuBuilder, MenuTextSize size = MenuTextSize.Medium)
    {
        _items.Add(new SubmenuMenuItem(text, submenuBuilder, size));
        return this;
    }

    public MenuBuilder AddSubmenu(string text, Func<IMenu> submenuBuilder)
    {
        return AddSubmenu(text, submenuBuilder, MenuTextSize.Medium);
    }

    public MenuBuilder AddSeparator()
    {
        _items.Add(new SeparatorMenuItem());
        return this;
    }

    public MenuBuilder AddText(string text, TextAlign alignment = TextAlign.Left, MenuTextSize size = MenuTextSize.Medium)
    {
        _items.Add(new TextMenuItem(text, alignment, size));
        return this;
    }

    public MenuBuilder AddDynamicText(Func<string> textProvider, TextAlign alignment = TextAlign.Left, MenuTextSize size = MenuTextSize.Medium)
    {
        _items.Add(new TextMenuItem(textProvider, alignment, size));
        return this;
    }

    public MenuBuilder WithParent(IMenu parent)
    {
        _parent = parent;
        return this;
    }

    public MenuBuilder VisibleWhen(Func<CCSPlayerController, bool> condition)
    {
        if (_items.Count > 0 && _items[^1] is ButtonMenuItem button)
        {
            button.VisibilityCheck = condition;
        }
        else if (_items.Count > 0 && _items[^1] is ToggleMenuItem toggle)
        {
            toggle.VisibilityCheck = condition;
        }
        else if (_items.Count > 0 && _items[^1] is SliderMenuItem slider)
        {
            slider.VisibilityCheck = condition;
        }
        else if (_items.Count > 0 && _items[^1] is ChoiceMenuItem choice)
        {
            choice.VisibilityCheck = condition;
        }
        else if (_items.Count > 0 && _items[^1] is SubmenuMenuItem submenu)
        {
            submenu.VisibilityCheck = condition;
        }
        else if (_items.Count > 0 && _items[^1] is TextMenuItem textItem)
        {
            textItem.VisibilityCheck = condition;
        }
        return this;
    }

    public MenuBuilder EnabledWhen(Func<CCSPlayerController, bool> condition)
    {
        if (_items.Count > 0 && _items[^1] is ButtonMenuItem button)
        {
            button.EnabledCheck = condition;
        }
        else if (_items.Count > 0 && _items[^1] is ToggleMenuItem toggle)
        {
            toggle.EnabledCheck = condition;
        }
        else if (_items.Count > 0 && _items[^1] is SliderMenuItem slider)
        {
            slider.EnabledCheck = condition;
        }
        else if (_items.Count > 0 && _items[^1] is ChoiceMenuItem choice)
        {
            choice.EnabledCheck = condition;
        }
        else if (_items.Count > 0 && _items[^1] is SubmenuMenuItem submenu)
        {
            submenu.EnabledCheck = condition;
        }
        return this;
    }

    public MenuBuilder RequirePermission(string permission)
    {
        return EnabledWhen(player =>
        {
            return AdminManager.PlayerHasPermissions(player, permission) || AdminManager.PlayerHasPermissions(player, "@css/root");
        });
    }

    public MenuBuilder WithValidation(Func<CCSPlayerController, bool> validation, Action<CCSPlayerController>? onFailed = null)
    {
        if (_items.Count > 0 && _items[^1] is ButtonMenuItem button)
        {
            button.ValidationCheck = validation;
            button.OnValidationFailed = onFailed;
        }
        else if (_items.Count > 0 && _items[^1] is ToggleMenuItem toggle)
        {
            toggle.ValidationCheck = validation;
            toggle.OnValidationFailed = onFailed;
        }
        else if (_items.Count > 0 && _items[^1] is AsyncButtonMenuItem asyncButton)
        {
            asyncButton.ValidationCheck = validation;
            asyncButton.OnValidationFailed = onFailed;
        }
        else if (_items.Count > 0 && _items[^1] is DynamicMenuItem dynamic)
        {
            dynamic.WithValidation(validation, onFailed);
        }
        return this;
    }

    public MenuBuilder CloseOnSelect()
    {
        if (_items.Count > 0 && _items[^1] is ButtonMenuItem button)
        {
            button.CloseOnSelect = true;
        }
        else if (_items.Count > 0 && _items[^1] is ToggleMenuItem toggle)
        {
            toggle.CloseOnSelect = true;
        }
        else if (_items.Count > 0 && _items[^1] is AsyncButtonMenuItem asyncButton)
        {
            asyncButton.CloseOnSelect = true;
        }
        else if (_items.Count > 0 && _items[^1] is DynamicMenuItem dynamic)
        {
            dynamic.WithCloseOnSelect(true);
        }
        return this;
    }

    public MenuBuilder AddAsyncButton(string text, Func<CCSPlayerController, Task> onClickAsync, MenuTextSize size = MenuTextSize.Medium)
    {
        _items.Add(new AsyncButtonMenuItem(text, onClickAsync, size));
        return this;
    }

    public MenuBuilder AddAsyncButton(string text, Func<CCSPlayerController, Task> onClickAsync)
    {
        return AddAsyncButton(text, onClickAsync, MenuTextSize.Medium);
    }

    public MenuBuilder AddDynamic(Func<string> textProvider, TimeSpan updateInterval, Action<CCSPlayerController>? onClick = null, MenuTextSize size = MenuTextSize.Medium)
    {
        _items.Add(new DynamicMenuItem(textProvider, updateInterval, onClick, size));
        return this;
    }

    public MenuBuilder AddDynamic(Func<string> textProvider, TimeSpan updateInterval, Action<CCSPlayerController>? onClick)
    {
        return AddDynamic(textProvider, updateInterval, onClick, MenuTextSize.Medium);
    }

    public MenuBuilder AddDynamic(Func<CCSPlayerController, string> textProvider, TimeSpan updateInterval, Action<CCSPlayerController>? onClick = null, MenuTextSize size = MenuTextSize.Medium)
    {
        _items.Add(new DynamicMenuItem(textProvider, updateInterval, onClick, size));
        return this;
    }

    public MenuBuilder AddDynamic(Func<CCSPlayerController, string> textProvider, TimeSpan updateInterval, Action<CCSPlayerController>? onClick)
    {
        return AddDynamic(textProvider, updateInterval, onClick, MenuTextSize.Medium);
    }

    public MenuBuilder AddProgressBar(string text, Func<float> progressProvider, int barWidth = 20, MenuTextSize size = MenuTextSize.Medium)
    {
        _items.Add(new ProgressBarMenuItem(text, progressProvider, barWidth, size));
        return this;
    }

    public MenuBuilder AddProgressBar(string text, Func<float> progressProvider, int barWidth)
    {
        return AddProgressBar(text, progressProvider, barWidth, MenuTextSize.Medium);
    }

    public MenuBuilder AutoClose(TimeSpan time)
    {
        _autoCloseTime = time;
        return this;
    }

    public MenuBuilder OverrideButtons(Action<MenuButtonOverrides> configureOverrides)
    {
        _buttonOverrides ??= new MenuButtonOverrides();
        configureOverrides(_buttonOverrides);
        return this;
    }

    public MenuBuilder OverrideSelectButton(params string[] buttonNames)
    {
        _buttonOverrides ??= new MenuButtonOverrides();
        _buttonOverrides.Select = MenuButtonOverrides.ParseButtons(buttonNames);
        return this;
    }

    public MenuBuilder OverrideBackButton(params string[] buttonNames)
    {
        _buttonOverrides ??= new MenuButtonOverrides();
        _buttonOverrides.Back = MenuButtonOverrides.ParseButtons(buttonNames);
        return this;
    }


    public MenuBuilder OverrideExitButton(params string[] buttonNames)
    {
        _buttonOverrides ??= new MenuButtonOverrides();
        _buttonOverrides.Exit = MenuButtonOverrides.ParseButtons(buttonNames);
        return this;
    }

    public MenuBuilder MaxVisibleItems(int count)
    {
        _maxVisibleItems = Math.Max(1, count); // Ensure at least 1 item is visible
        return this;
    }

    public MenuBuilder NoFreeze()
    {
        _freezeOverride = false;
        return this;
    }

    public MenuBuilder ForceFreeze()
    {
        _freezeOverride = true;
        return this;
    }

    public Menu Build()
    {
        var menu = new Menu(_title);
        menu.Parent = _parent;
        menu.AutoCloseTime = _autoCloseTime;
        menu.ButtonOverrides = _buttonOverrides;
        menu.MaxVisibleItems = _maxVisibleItems;
        menu.FreezeOverride = _freezeOverride;

        foreach (var item in _items)
        {
            menu.Items.Add(item);
        }

        return menu;
    }
}