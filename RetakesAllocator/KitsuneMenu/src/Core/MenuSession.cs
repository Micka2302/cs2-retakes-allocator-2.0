using KitsuneMenu.Core.Interfaces;

namespace KitsuneMenu.Core;

public class MenuSession
{
    public Dictionary<string, object> Data { get; } = new();
    public Stack<IMenu> History { get; } = new();
    public Stack<int> SelectionHistory { get; } = new();
    public IMenu? CurrentMenu { get; set; }
    public int SelectedIndex { get; set; }
    public int ViewportOffset { get; set; } = 0;
    public DateTime LastInteraction { get; set; } = DateTime.Now;
    public bool NeedsRender { get; set; } = true;
    public string? CachedHtml { get; set; }
    public int LastItemCount { get; set; }
    public bool WaitingForGround { get; set; } = false;
    public DateTime MenuOpenTime { get; set; } = DateTime.Now;

    public T Get<T>(string key) where T : notnull
    {
        if (Data.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }

        throw new KeyNotFoundException($"Key '{key}' not found in session data");
    }

    public T Get<T>(string key, T defaultValue) where T : notnull
    {
        if (Data.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }

        return defaultValue;
    }

    public void Set<T>(string key, T value) where T : notnull
    {
        Data[key] = value;
    }

    public bool TryGet<T>(string key, out T? value) where T : notnull
    {
        if (Data.TryGetValue(key, out var objValue) && objValue is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    public void Remove(string key)
    {
        Data.Remove(key);
    }

    public void Clear()
    {
        Data.Clear();
        History.Clear();
        SelectionHistory.Clear();
        CurrentMenu = null;
        SelectedIndex = 0;
        ViewportOffset = 0;
        WaitingForGround = false;
    }

    public void NavigateTo(IMenu menu)
    {
        if (CurrentMenu != null)
        {
            History.Push(CurrentMenu);
            SelectionHistory.Push(SelectedIndex); // Save current selection
        }

        CurrentMenu = menu;
        SelectedIndex = -1; // Force reselection in ProcessPlayerInput
        ViewportOffset = 0; // Reset viewport when opening new menu
        NeedsRender = true;
        CachedHtml = null;
        MenuOpenTime = DateTime.Now; // Reset open time for auto-close
    }

    public void GoBack()
    {
        if (History.Count > 0)
        {
            CurrentMenu = History.Pop();
            SelectedIndex = SelectionHistory.Count > 0 ? SelectionHistory.Pop() : 0; // Restore previous selection
            NeedsRender = true;
            CachedHtml = null;
        }
        else
        {
            CurrentMenu = null;
        }
    }
}