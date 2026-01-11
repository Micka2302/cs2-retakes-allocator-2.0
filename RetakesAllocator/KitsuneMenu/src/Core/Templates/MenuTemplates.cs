using CounterStrikeSharp.API.Core;
using KitsuneMenu.Core.Interfaces;
using KitsuneMenu.Core.MenuItems;

namespace KitsuneMenu.Core.Templates;

public static class MenuTemplates
{
    public static IMenu Confirm(string title, string message, Action<CCSPlayerController> onConfirm, Action<CCSPlayerController>? onCancel = null)
    {
        return new MenuBuilder(title)
            .AddText(message, TextAlign.Center)
            .AddSeparator()
            .AddButton("✓ Yes", player =>
            {
                onConfirm(player);
                KitsuneMenu.CloseMenu(player);
            })
            .AddButton("✗ No", player =>
            {
                onCancel?.Invoke(player);
                KitsuneMenu.CloseMenu(player);
            })
            .Build();
    }

    public static IMenu Alert(string title, string message, Action<CCSPlayerController>? onOk = null)
    {
        return new MenuBuilder(title)
            .AddText(message, TextAlign.Center)
            .AddSeparator()
            .AddButton("OK", player =>
            {
                onOk?.Invoke(player);
                KitsuneMenu.CloseMenu(player);
            })
            .Build();
    }

    public static IMenu NumberPicker(string title, int min, int max, int defaultValue, Action<CCSPlayerController, int> onNumberSelected, int[]? steps = null)
    {
        steps ??= new[] { 1, 10 };
        Array.Sort(steps);

        var menu = new MenuBuilder(title);

        var currentValue = defaultValue;

        menu.AddText($"Current: {currentValue}", TextAlign.Center);
        menu.AddSeparator();

        // Add increment buttons for each step
        for (int i = steps.Length - 1; i >= 0; i--)
        {
            var step = steps[i];
            menu.AddButton($"+ {step}", player =>
            {
                currentValue = Math.Min(currentValue + step, max);
                RefreshNumberPicker(player, title, min, max, currentValue, onNumberSelected, steps);
            }).EnabledWhen(_ => currentValue + step <= max);
        }

        menu.AddText($"──── {currentValue} ────", TextAlign.Center);

        // Add decrement buttons for each step
        for (int i = 0; i < steps.Length; i++)
        {
            var step = steps[i];
            menu.AddButton($"- {step}", player =>
            {
                currentValue = Math.Max(currentValue - step, min);
                RefreshNumberPicker(player, title, min, max, currentValue, onNumberSelected, steps);
            }).EnabledWhen(_ => currentValue - step >= min);
        }

        menu.AddSeparator();

        menu.AddButton("Confirm", player =>
        {
            onNumberSelected(player, currentValue);
            KitsuneMenu.CloseMenu(player);
        });

        menu.AddButton("Cancel", player => KitsuneMenu.CloseMenu(player));

        return menu.Build();
    }

    public static IMenu List<T>(string title, T[] items, Func<T, string> displaySelector, Action<CCSPlayerController, T> onItemSelected)
    {
        var menu = new MenuBuilder(title);

        foreach (var item in items)
        {
            var display = displaySelector(item);
            menu.AddButton(display, player =>
            {
                onItemSelected(player, item);
            });
        }

        if (items.Length == 0)
        {
            menu.AddText("No items available", TextAlign.Center);
        }

        menu.AddSeparator();
        menu.AddButton("Back", player =>
        {
            if (KitsuneMenu.TryGetSession(player, out var session))
            {
                session.GoBack();
            }
        });

        return menu.Build();
    }

    private static void RefreshNumberPicker(CCSPlayerController player, string title, int min, int max, int currentValue, Action<CCSPlayerController, int> onNumberSelected, int[]? steps = null)
    {
        var newMenu = NumberPicker(title, min, max, currentValue, onNumberSelected, steps);
        newMenu.Show(player);
    }
}