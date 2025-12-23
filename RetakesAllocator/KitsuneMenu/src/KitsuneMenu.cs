using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using KitsuneMenu.Core;
using KitsuneMenu.Core.Interfaces;
using KitsuneMenu.Core.MenuItems;

namespace KitsuneMenu;

public static class KitsuneMenu
{
    public static void Init()
    {
        GlobalMenuManager.EnsureInitialized();
    }

    public static void Cleanup()
    {
        GlobalTickHandler.Cleanup();
    }

    public static MenuBuilder Create(string title)
    {
        return new MenuBuilder(title);
    }

    public static void ShowMenu(CCSPlayerController player, IMenu menu)
    {
        if (!player.IsValid || player.IsBot) return;

        var session = GlobalMenuManager.GetOrCreateSession(player);
        session.NavigateTo(menu);

        // Find and select first selectable item (even if disabled)
        var visibleItems = menu.Items.Where(item => item.ShouldShow(player)).ToList();
        for (int i = 0; i < visibleItems.Count; i++)
        {
            if (IsSelectable(visibleItems[i]))
            {
                session.SelectedIndex = i;
                break;
            }
        }

        // Check if freezing is enabled (per-menu override or global config)
        var shouldFreeze = menu.FreezeOverride ?? GlobalMenuManager.Config.FreezePlayer;

        if (menu.Parent == null && shouldFreeze)
        {
            // Mark session as waiting for ground if player is in air
            var pawn = player.PlayerPawn?.Value;
            if (pawn != null && pawn.IsValid && !pawn.OnGroundLastTick)
            {
                session.WaitingForGround = true;
            }
            else
            {
                FreezePlayer(player, true);
            }
        }
    }

    public static void CloseMenu(CCSPlayerController player)
    {
        if (!player.IsValid) return;

        // Get the menu before closing to check freeze override
        if (GlobalMenuManager.TryGetSession(player, out var session))
        {
            var shouldFreeze = session.CurrentMenu?.FreezeOverride ?? GlobalMenuManager.Config.FreezePlayer;
            if (shouldFreeze)
            {
                FreezePlayer(player, false);
            }
        }

        GlobalMenuManager.CloseSession(player);
    }

    public static bool TryGetSession(CCSPlayerController player, out MenuSession session)
    {
        return GlobalMenuManager.TryGetSession(player, out session);
    }

    /// <summary>
    /// Close all menus with a specific title
    /// </summary>
    /// <param name="title">The menu title to search for</param>
    /// <param name="exactMatch">If true, requires exact match. If false, uses case-insensitive contains</param>
    public static void CloseMenusByTitle(string title, bool exactMatch = false)
    {
        GlobalMenuManager.CloseMenusByTitle(title, exactMatch);
    }

    /// <summary>
    /// Close all menus that match a predicate condition
    /// </summary>
    /// <param name="predicate">Function to test each menu</param>
    public static void CloseAllMenusWhere(Func<IMenu, bool> predicate)
    {
        GlobalMenuManager.CloseAllMenusWhere(predicate);
    }

    /// <summary>
    /// Close a specific player's menu if it matches the predicate
    /// </summary>
    public static void CloseMenuForPlayer(CCSPlayerController player, Func<IMenu, bool> predicate)
    {
        GlobalMenuManager.CloseMenusForPlayer(player, predicate);
    }

    internal static void HandleItemSelection(CCSPlayerController player, MenuSession session, IMenuItem item)
    {
        var menu = session.CurrentMenu;
        if (menu == null) return;

        if (menu is Menu concreteMenu)
            concreteMenu.TriggerItemSelected(player, item);

        switch (item)
        {
            case ButtonMenuItem button:
                if (button.ValidationCheck != null && !button.ValidationCheck(player))
                {
                    button.OnValidationFailed?.Invoke(player);
                    return;
                }
                button.OnClick?.Invoke(player);
                if (button.CloseOnSelect)
                {
                    CloseMenu(player);
                }
                break;

            case AsyncButtonMenuItem asyncButton:
                if (asyncButton.ValidationCheck != null && !asyncButton.ValidationCheck(player))
                {
                    asyncButton.OnValidationFailed?.Invoke(player);
                    return;
                }

                // Set loading state immediately and re-render
                asyncButton.IsLoading = true;
                asyncButton.SetLoadingText("Processing...");
                session.NeedsRender = true;

                var closeAfter = asyncButton.CloseOnSelect;
                Task.Run(async () =>
                {
                    try
                    {
                        await asyncButton.ExecuteAsync(player, "Processing...");
                    }
                    finally
                    {
                        // Clear loading state and trigger re-render
                        asyncButton.IsLoading = false;
                        session.NeedsRender = true;

                        // Close menu after async operation if requested
                        if (closeAfter && player.IsValid)
                        {
                            CloseMenu(player);
                        }
                    }
                });
                break;

            case ToggleMenuItem toggle:
                toggle.Toggle(player);
                if (toggle.CloseOnSelect)
                {
                    CloseMenu(player);
                }
                else
                {
                    session.NeedsRender = true;
                }
                break;

            case SubmenuMenuItem submenu:
                var subMenu = submenu.GetSubmenu();
                if (subMenu != null)
                {
                    subMenu.Parent = menu;
                    ShowMenu(player, subMenu);
                }
                break;

            case DynamicMenuItem dynamic:
                dynamic.Click(player);
                if (dynamic.ShouldCloseOnSelect())
                {
                    CloseMenu(player);
                }
                break;
        }
    }

    internal static bool HandleLeftRight(CCSPlayerController player, IMenuItem? item, bool isRight)
    {
        if (item == null) return false;

        switch (item)
        {
            case SliderMenuItem slider:
                if (isRight) slider.Increase(player);
                else slider.Decrease(player);
                return true;

            case ChoiceMenuItem choice:
                if (isRight) choice.Next(player);
                else choice.Previous(player);
                return true;
        }

        return false;
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

    internal static void FreezePlayer(CCSPlayerController player, bool freeze)
    {
        if (!player.IsValid) return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return;

        var moveType = freeze ? MoveType_t.MOVETYPE_NONE : MoveType_t.MOVETYPE_WALK;

        pawn.MoveType = moveType;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
        Schema.GetRef<MoveType_t>(pawn.Handle, "CBaseEntity", "m_nActualMoveType") = moveType;
    }
}