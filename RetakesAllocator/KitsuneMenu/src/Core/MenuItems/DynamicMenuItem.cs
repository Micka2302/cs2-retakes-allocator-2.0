using CounterStrikeSharp.API.Core;
using KitsuneMenu.Core.Interfaces;
using KitsuneMenu.Core.Enums;

namespace KitsuneMenu.Core.MenuItems;

public class DynamicMenuItem : IMenuItem
{
    private readonly Func<CCSPlayerController, string> _textProvider;
    private readonly Action<CCSPlayerController>? _onClick;
    private Func<CCSPlayerController, bool>? _visibilityCheck;
    private Func<CCSPlayerController, bool>? _enabledCheck;
    private Func<CCSPlayerController, bool>? _validationCheck;
    private Action<CCSPlayerController>? _onValidationFailed;
    private readonly TimeSpan _updateInterval;
    private DateTime _lastUpdate = DateTime.MinValue;
    private string _cachedText = "";
    private MenuTextSize _size;
    private bool _closeOnSelect;

    public string Text
    {
        get => _cachedText;
        set => _cachedText = value;
    }

    public bool Visible => true;
    public bool Enabled => true;

    public DynamicMenuItem(Func<CCSPlayerController, string> textProvider, TimeSpan updateInterval, Action<CCSPlayerController>? onClick = null, MenuTextSize size = MenuTextSize.Medium)
    {
        _textProvider = textProvider;
        _updateInterval = updateInterval;
        _onClick = onClick;
        _size = size;
    }

    public DynamicMenuItem(Func<string> textProvider, TimeSpan updateInterval, Action<CCSPlayerController>? onClick = null, MenuTextSize size = MenuTextSize.Medium)
    {
        _textProvider = _ => textProvider();
        _updateInterval = updateInterval;
        _onClick = onClick;
        _size = size;
    }

    public bool ShouldShow(CCSPlayerController player)
    {
        return _visibilityCheck?.Invoke(player) ?? true;
    }

    public bool CanInteract(CCSPlayerController player)
    {
        return _onClick != null && (_enabledCheck?.Invoke(player) ?? true);
    }

    public string GetDisplayText(CCSPlayerController player)
    {
        var sizeClass = Utils.MenuSizeHelper.GetCssClass(_size);

        var needsUpdate = DateTime.Now - _lastUpdate > _updateInterval;
        if (needsUpdate)
        {
            var oldText = _cachedText;
            _cachedText = _textProvider(player);
            _lastUpdate = DateTime.Now;

            // Notify menu system if text changed
            if (oldText != _cachedText && global::KitsuneMenu.KitsuneMenu.TryGetSession(player, out var session))
            {
                session.NeedsRender = true;
            }
        }

        if (!CanInteract(player) && _onClick != null)
        {
            return $"<font class='{sizeClass}' color='#8f3b3b'>{_cachedText}</font>";
        }

        return $"<font class='{sizeClass}'>{_cachedText}</font>";
    }
    
    public MenuTextSize GetTextSize()
    {
        return _size;
    }

    public void Click(CCSPlayerController player)
    {
        if (CanInteract(player))
        {
            if (_validationCheck != null && !_validationCheck(player))
            {
                _onValidationFailed?.Invoke(player);
                return;
            }
            _onClick?.Invoke(player);
        }
    }

    public DynamicMenuItem WithVisibilityCheck(Func<CCSPlayerController, bool> check)
    {
        _visibilityCheck = check;
        return this;
    }

    public DynamicMenuItem WithEnabledCheck(Func<CCSPlayerController, bool> check)
    {
        _enabledCheck = check;
        return this;
    }

    public DynamicMenuItem WithValidation(Func<CCSPlayerController, bool> check, Action<CCSPlayerController>? onFailed = null)
    {
        _validationCheck = check;
        _onValidationFailed = onFailed;
        return this;
    }

    public DynamicMenuItem WithCloseOnSelect(bool close = true)
    {
        _closeOnSelect = close;
        return this;
    }

    public bool ShouldCloseOnSelect() => _closeOnSelect;
}