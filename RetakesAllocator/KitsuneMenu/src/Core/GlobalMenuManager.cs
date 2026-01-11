using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using KitsuneMenu.Core.Interfaces;
using KitsuneMenu.Core.MenuItems;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KitsuneMenu.Core;

internal static class GlobalMenuManager
{
    private static readonly ConcurrentDictionary<uint, MenuSession> _playerSessions = new();
    private static readonly ConcurrentDictionary<uint, DateTime> _lastButtonPress = new();
    private static readonly ConcurrentDictionary<uint, PlayerButtons> _lastButtons = new();
    private static readonly ConcurrentDictionary<uint, KeyRepeatInfo> _keyRepeatInfo = new();
    private static readonly object _initLock = new();
    private static bool _isInitialized = false;
    private static MenuConfig _config = new();
    private static MenuTranslations? _translations;

    public static void EnsureInitialized()
    {
        lock (_initLock)
        {
            if (_isInitialized) return;

            _config = MenuConfig.Load();
            _translations = MenuTranslations.Load();

            // Initialize global tick handler
            GlobalTickHandler.Initialize();

            _isInitialized = true;
        }
    }

    public static MenuSession GetOrCreateSession(CCSPlayerController player)
    {
        return _playerSessions.GetOrAdd(player.Index, _ => new MenuSession());
    }

    public static bool TryGetSession(CCSPlayerController player, out MenuSession session)
    {
        return _playerSessions.TryGetValue(player.Index, out session!);
    }

    public static void CloseSession(CCSPlayerController player)
    {
        if (_playerSessions.TryRemove(player.Index, out var session))
        {
            session.Clear();
        }
        // Also clean up related data
        _lastButtonPress.TryRemove(player.Index, out _);
        _lastButtons.TryRemove(player.Index, out _);
        _keyRepeatInfo.TryRemove(player.Index, out _);
    }

    public static MenuConfig Config => _config;
    public static MenuTranslations Translations => _translations ?? MenuTranslations.Load();

    public static void Cleanup()
    {
        // Close all active menus
        foreach (var kvp in _playerSessions)
        {
            var player = Utilities.GetPlayerFromIndex((int)kvp.Key);
            if (player != null && player.IsValid)
            {
                KitsuneMenu.CloseMenu(player);
            }
        }

        _playerSessions.Clear();
        _lastButtonPress.Clear();
        _lastButtons.Clear();
        _keyRepeatInfo.Clear();
    }

    public static void CloseMenusByTitle(string title, bool exactMatch = false)
    {
        var sessionsToClose = new List<CCSPlayerController>();

        foreach (var kvp in _playerSessions)
        {
            var session = kvp.Value;
            if (session.CurrentMenu == null) continue;

            bool shouldClose = exactMatch 
                ? session.CurrentMenu.Title == title
                : session.CurrentMenu.Title.Contains(title, StringComparison.OrdinalIgnoreCase);

            if (shouldClose)
            {
                var player = Utilities.GetPlayerFromIndex((int)kvp.Key);
                if (player != null && player.IsValid)
                {
                    sessionsToClose.Add(player);
                }
            }
        }

        // Close menus outside of iteration
        foreach (var player in sessionsToClose)
        {
            KitsuneMenu.CloseMenu(player);
        }
    }

    public static void CloseMenusForPlayer(CCSPlayerController player, Func<IMenu, bool> predicate)
    {
        if (!player.IsValid) return;

        if (_playerSessions.TryGetValue(player.Index, out var session))
        {
            if (session.CurrentMenu != null && predicate(session.CurrentMenu))
            {
                KitsuneMenu.CloseMenu(player);
            }
        }
    }

    public static void CloseAllMenusWhere(Func<IMenu, bool> predicate)
    {
        var sessionsToClose = new List<CCSPlayerController>();

        foreach (var kvp in _playerSessions)
        {
            var session = kvp.Value;
            if (session.CurrentMenu != null && predicate(session.CurrentMenu))
            {
                var player = Utilities.GetPlayerFromIndex((int)kvp.Key);
                if (player != null && player.IsValid)
                {
                    sessionsToClose.Add(player);
                }
            }
        }

        // Close menus outside of iteration
        foreach (var player in sessionsToClose)
        {
            KitsuneMenu.CloseMenu(player);
        }
    }

    public static void OnGlobalTick()
    {
        // Only process players with active sessions to avoid unnecessary iterations
        var sessionsToRemove = new List<uint>();
        
        foreach (var kvp in _playerSessions)
        {
            var session = kvp.Value;
            var player = Utilities.GetPlayerFromIndex((int)kvp.Key);
            
            // Clean up sessions for disconnected players or empty sessions
            if (player == null || !player.IsValid || player.IsBot)
            {
                // Player disconnected or invalid - mark for removal
                sessionsToRemove.Add(kvp.Key);
                continue;
            }
            
            // Clean up empty sessions that have been idle for too long
            if (session.CurrentMenu == null)
            {
                var idleTime = DateTime.Now - session.LastInteraction;
                if (idleTime > TimeSpan.FromMinutes(5))
                {
                    sessionsToRemove.Add(kvp.Key);
                }
                continue;
            }

            // Check if waiting for ground
            if (session.WaitingForGround && _config.FreezePlayer)
            {
                var pawn = player.PlayerPawn?.Value;
                if (pawn != null && pawn.IsValid && pawn.OnGroundLastTick)
                {
                    session.WaitingForGround = false;
                    KitsuneMenu.FreezePlayer(player, true);
                }
            }

            // Check for auto-close
            if (session.CurrentMenu?.AutoCloseTime != null)
            {
                var elapsed = DateTime.Now - session.MenuOpenTime;
                if (elapsed >= session.CurrentMenu.AutoCloseTime.Value)
                {
                    KitsuneMenu.CloseMenu(player);
                    continue;
                }
            }

            ProcessPlayerInput(player, session);
            RenderMenu(player, session);
        }
        
        // Clean up disconnected players
        foreach (var playerId in sessionsToRemove)
        {
            if (_playerSessions.TryRemove(playerId, out var removedSession))
            {
                // Properly clean up the session
                removedSession.Clear();
            }
            _lastButtonPress.TryRemove(playerId, out _);
            _lastButtons.TryRemove(playerId, out _);
            _keyRepeatInfo.TryRemove(playerId, out _);
        }
    }

    private static void ProcessPlayerInput(CCSPlayerController player, MenuSession session)
    {
        var buttons = player.Buttons;
        var lastButton = _lastButtons.GetValueOrDefault(player.Index);
        var pressedButtons = buttons & ~lastButton;
        _lastButtons[player.Index] = buttons;
        
        // Update last interaction time when player presses any button
        if (pressedButtons != 0)
        {
            session.LastInteraction = DateTime.Now;
        }

        var now = DateTime.Now;
        var repeatInfo = _keyRepeatInfo.GetOrAdd(player.Index, _ => new KeyRepeatInfo());

        // Check if we should process a repeat
        bool shouldProcess = false;
        PlayerButtons buttonToProcess = 0;

        if (pressedButtons != 0)
        {
            // New button press
            shouldProcess = true;
            buttonToProcess = pressedButtons;

            // Reset repeat info for new button
            repeatInfo.LastButton = (ulong)buttons;
            repeatInfo.FirstPressTime = now;
            repeatInfo.LastRepeatTime = now;
            repeatInfo.RepeatCount = 0;
        }
        else if (buttons != 0 && (ulong)buttons == repeatInfo.LastButton)
        {
            // Key is being held - check for repeat
            var holdDuration = (now - repeatInfo.FirstPressTime).TotalMilliseconds;
            var timeSinceLastRepeat = (now - repeatInfo.LastRepeatTime).TotalMilliseconds;

            // Initial delay: 500ms, then repeat based on count
            var initialDelay = 500;
            var repeatDelay = Math.Max(30, 150 - (repeatInfo.RepeatCount * 10)); // Speed up from 150ms to 30ms

            if (holdDuration > initialDelay && timeSinceLastRepeat >= repeatDelay)
            {
                shouldProcess = true;
                buttonToProcess = buttons;
                repeatInfo.LastRepeatTime = now;
                repeatInfo.RepeatCount++;
            }
        }
        else if (buttons == 0)
        {
            // All buttons released
            repeatInfo.Reset();
        }

        if (!shouldProcess) return;

        var menu = session.CurrentMenu;
        if (menu == null) return;

        var visibleItems = menu.Items.Where(item => item.ShouldShow(player)).ToList();
        if (visibleItems.Count == 0) return;

        // Always ensure selected index is on a selectable item
        bool needsNewSelection = session.SelectedIndex < 0 ||
                                session.SelectedIndex >= visibleItems.Count ||
                                !IsSelectable(visibleItems[session.SelectedIndex]);

        if (needsNewSelection)
        {
            // Find first selectable item
            for (int i = 0; i < visibleItems.Count; i++)
            {
                if (IsSelectable(visibleItems[i]))
                {
                    session.SelectedIndex = i;
                    break;
                }
            }
        }

        // Get effective buttons (with overrides if present)
        var selectButtons = menu.ButtonOverrides?.Select ?? _config.SelectButtons;
        var backButtons = menu.ButtonOverrides?.Back ?? _config.BackButtons;
        var exitButtons = menu.ButtonOverrides?.Exit ?? _config.ExitButtons;

        // WASD navigation is hardcoded
        if (HasButton(buttonToProcess, PlayerButtons.Forward))
        {
            // Try to find previous selectable item
            var foundSelectable = false;
            var newIndex = session.SelectedIndex - 1;
            while (newIndex >= 0)
            {
                var item = visibleItems.ElementAtOrDefault(newIndex);
                if (item != null && IsSelectable(item))
                {
                    session.SelectedIndex = newIndex;
                    foundSelectable = true;
                    session.NeedsRender = true;
                    if (menu is Menu concreteMenu) concreteMenu.TriggerItemHovered(player, item);
                    break;
                }
                newIndex--;
            }
            
            // If no selectable item found, still scroll the viewport if possible
            if (!foundSelectable && session.ViewportOffset > 0)
            {
                session.ViewportOffset--;
                session.NeedsRender = true;
            }
        }
        else if (HasButton(buttonToProcess, PlayerButtons.Back))
        {
            // Try to find next selectable item
            var foundSelectable = false;
            var newIndex = session.SelectedIndex + 1;
            while (newIndex < visibleItems.Count)
            {
                var item = visibleItems.ElementAtOrDefault(newIndex);
                if (item != null && IsSelectable(item))
                {
                    session.SelectedIndex = newIndex;
                    foundSelectable = true;
                    session.NeedsRender = true;
                    if (menu is Menu concreteMenu) concreteMenu.TriggerItemHovered(player, item);
                    break;
                }
                newIndex++;
            }
            
            // If no selectable item found, still scroll the viewport if possible
            if (!foundSelectable && session.ViewportOffset + menu.MaxVisibleItems < visibleItems.Count)
            {
                session.ViewportOffset++;
                session.NeedsRender = true;
            }
        }
        else if (HasButton(buttonToProcess, selectButtons))
        {
            var item = visibleItems.ElementAtOrDefault(session.SelectedIndex);
            if (item != null && item.CanInteract(player))
            {
                KitsuneMenu.HandleItemSelection(player, session, item);
            }
        }
        else if (HasButton(buttonToProcess, PlayerButtons.Moveleft))
        {
            var item = visibleItems.ElementAtOrDefault(session.SelectedIndex);
            if (KitsuneMenu.HandleLeftRight(player, item, false))
                session.NeedsRender = true;
        }
        else if (HasButton(buttonToProcess, PlayerButtons.Moveright))
        {
            var item = visibleItems.ElementAtOrDefault(session.SelectedIndex);
            if (KitsuneMenu.HandleLeftRight(player, item, true))
                session.NeedsRender = true;
        }
        else if (HasButton(buttonToProcess, backButtons))
        {
            if (menu.Parent != null)
            {
                session.GoBack();
                if (menu is Menu concreteMenu)
                    concreteMenu.TriggerBack(player);
            }
            else
            {
                KitsuneMenu.CloseMenu(player);
            }
        }
        else if (HasButton(buttonToProcess, exitButtons))
        {
            KitsuneMenu.CloseMenu(player);
        }
    }

    private static void RenderMenu(CCSPlayerController player, MenuSession session)
    {
        var menu = session.CurrentMenu;
        if (menu == null) return;

        // First check if we can use cached HTML (before checking dynamic items)
        if (!session.NeedsRender && session.CachedHtml != null)
        {
            // Check if any dynamic items need updating
            var needsUpdate = false;
            foreach (var item in menu.Items)
            {
                if (item is DynamicMenuItem dynamicItem && item.ShouldShow(player))
                {
                    // Store old NeedsRender state
                    var oldNeedsRender = session.NeedsRender;
                    
                    // Call GetDisplayText to trigger update check
                    dynamicItem.GetDisplayText(player);
                    
                    // If the dynamic item triggered a render, we need to update
                    if (session.NeedsRender && !oldNeedsRender)
                    {
                        needsUpdate = true;
                    }
                }
            }

            // If no updates needed, use cached HTML
            if (!needsUpdate)
            {
                player.PrintToCenterHtml(session.CachedHtml);
                return;
            }
        }

        // Need to rebuild the menu HTML
        session.NeedsRender = false;

        if (menu is Menu concreteMenu)
            concreteMenu.TriggerBeforeRender(player);

        var visibleItems = menu.Items.Where(item => item.ShouldShow(player)).ToList();
        session.LastItemCount = visibleItems.Count;
        var html = new StringBuilder();

        // Pagination settings
        var maxVisibleItems = menu.MaxVisibleItems;
        var totalItems = visibleItems.Count;
        
        // Update viewport offset based on selection
        if (session.SelectedIndex < session.ViewportOffset)
        {
            session.ViewportOffset = session.SelectedIndex;
        }
        else if (session.SelectedIndex >= session.ViewportOffset + maxVisibleItems)
        {
            session.ViewportOffset = session.SelectedIndex - maxVisibleItems + 1;
        }
        
        var startIndex = session.ViewportOffset;
        var endIndex = Math.Min(startIndex + maxVisibleItems, totalItems);

        // Title section
        html.Append($"<font class='fontSize-m' color='#ff3333'>{menu.Title}</font>");

        // Item counter if scrollable
        if (totalItems > maxVisibleItems)
        {
            html.Append($"<font class='fontSize-s' color='#FFFFFF'> {string.Format(Translations.ItemsCounter, session.SelectedIndex + 1, totalItems)}</font>");
        }

        html.Append("<font color='#FFFFFF' class='fontSize-sm'><br>");

        // Render visible items
        for (int i = startIndex; i < endIndex; i++)
        {
            var item = visibleItems[i];
            var isSelected = i == session.SelectedIndex;
            var arrowSizeClass = Utils.MenuSizeHelper.GetCssClass(item.GetTextSize());

            if (isSelected)
            {
                html.Append($"<font color='#ff3333' class='{arrowSizeClass}'>► </font>");
            }
            else
            {
                html.Append("\u00A0\u00A0\u00A0 ");
            }

            html.Append(item.GetDisplayText(player));

            if (isSelected)
            {
                html.Append($"<font color='#ff3333' class='{arrowSizeClass}'> ◄</font>");
            }

            html.Append("<br>");
        }

        // Footer
        html.Append("<br>");

        if (menu.Parent != null)
        {
            // Submenu footer
            html.Append(BuildFooter(true, menu));
        }
        else
        {
            // Main menu footer
            html.Append(BuildFooter(false, menu));
        }

        html.Append("</font>");

        var finalHtml = html.ToString();
        session.CachedHtml = finalHtml;
        player.PrintToCenterHtml(finalHtml);

        if (menu is Menu concreteMenu2)
            concreteMenu2.TriggerAfterRender(player);
    }

    private static bool HasButton(PlayerButtons buttons, PlayerButtons check)
    {
        return (buttons & check) != 0;
    }
    
    private static bool IsSelectable(IMenuItem item)
    {
        // Items that can have cursor on them (even if disabled)
        return item is ButtonMenuItem || 
               item is ToggleMenuItem || 
               item is SliderMenuItem || 
               item is ChoiceMenuItem || 
               item is SubmenuMenuItem || 
               item is AsyncButtonMenuItem ||
               (item is DynamicMenuItem dynamic && dynamic.CanInteract(null!));
    }

    private static string BuildFooter(bool isSubmenu, IMenu menu)
    {
        var footer = new StringBuilder("<font color='#ff3333' class='fontSize-s'>");

        // Get effective buttons (with overrides if present)
        var selectButtons = menu.ButtonOverrides?.Select ?? _config.SelectButtons;
        var backButtons = menu.ButtonOverrides?.Back ?? _config.BackButtons;
        var exitButtons = menu.ButtonOverrides?.Exit ?? _config.ExitButtons;

        // Convert buttons to string lists for display
        var selectDisplay = GetButtonNamesFromFlags(selectButtons);
        var backDisplay = GetButtonNamesFromFlags(backButtons);
        var exitDisplay = GetButtonNamesFromFlags(exitButtons);

        // Select section (no Move section anymore, WASD is hardcoded)
        footer.Append($"{Translations.FooterSelect}: <font color='#f5a142'>{Translations.GetButtonsDisplay(selectDisplay)}");

        // Back section (only for submenus)
        if (isSubmenu)
        {
            footer.Append($"<font color='#FFFFFF'> | <font color='#ff3333'>{Translations.FooterBack}: <font color='#f5a142'>{Translations.GetButtonsDisplay(backDisplay)}");
        }

        // Exit section
        footer.Append($"<font color='#FFFFFF'> | <font color='#ff3333'>{Translations.FooterExit}: <font color='#f5a142'>{Translations.GetButtonsDisplay(exitDisplay)}");

        footer.Append("</font><br>");
        return footer.ToString();
    }

    private static List<string> GetButtonNamesFromFlags(PlayerButtons buttons)
    {
        var result = new List<string>();

        // Check special buttons first
        if ((buttons & (PlayerButtons)(1UL << 33)) != 0) result.Add("Scoreboard");
        if ((buttons & (PlayerButtons)(1UL << 35)) != 0) result.Add("Inspect");

        // Check standard buttons
        foreach (PlayerButtons button in Enum.GetValues(typeof(PlayerButtons)))
        {
            if (button == 0) continue;
            if ((buttons & button) != 0 && button < (PlayerButtons)(1UL << 32))
            {
                result.Add(button.ToString());
            }
        }

        return result;
    }

}

