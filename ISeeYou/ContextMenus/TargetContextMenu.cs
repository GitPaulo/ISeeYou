using System.Collections.Generic;
using Dalamud.Game.Gui.ContextMenu;

namespace ISeeYou.ContextMenus;

public class TargetContextMenu
{
    private readonly MenuItem registerPlayerMenuItem;
    private readonly MenuItem trackTargetingParentMenuItem;
    private readonly MenuItem unregisterPlayerMenuItem;
    private string? targetFullName;
    private ulong? targetObjectId;

    public TargetContextMenu()
    {
        registerPlayerMenuItem = new MenuItem
        {
            Name = "Register Target",
            OnClicked = OnRegisterPlayerClicked
        };

        unregisterPlayerMenuItem = new MenuItem
        {
            Name = "Unregister Target",
            OnClicked = OnUnregisterPlayerClicked
        };

        trackTargetingParentMenuItem = new MenuItem
        {
            Name = "Targets",
            PrefixChar = 'T',
            IsSubmenu = true,
            OnClicked = OpenTrackTargetingSubmenu
        };
    }

    public void Enable()
    {
        Shared.ContextMenu.OnMenuOpened += OnContextMenuOpened;
    }

    public void Disable()
    {
        Shared.ContextMenu.OnMenuOpened -= OnContextMenuOpened;
    }

    private static bool IsMenuValid(IMenuArgs menuOpenedArgs)
    {
        if (menuOpenedArgs.Target is not MenuTargetDefault menuTargetDefault) return false;

        switch (menuOpenedArgs.AddonName)
        {
            case null: // Nameplate/Model menu
            case "LookingForGroup":
            case "PartyMemberList":
            case "FriendList":
            case "FreeCompany":
            case "SocialList":
            case "ContactList":
            case "ChatLog":
            case "_PartyList":
            case "LinkShell":
            case "CrossWorldLinkshell":
            case "ContentMemberList": // Eureka/Bozja/...
            case "BeginnerChatList":
                return menuTargetDefault.TargetName != string.Empty &&
                       Util.IsWorldValid(menuTargetDefault.TargetHomeWorld.RowId);

            case "BlackList":
            case "MuteList":
                return menuTargetDefault.TargetName != string.Empty;
        }

        return false;
    }

    private void OnContextMenuOpened(IMenuOpenedArgs menuArgs)
    {
        if (menuArgs.Target is not MenuTargetDefault menuTargetDefault || !IsMenuValid(menuArgs))
            return;

        targetFullName = menuTargetDefault.TargetName;
        targetObjectId = menuTargetDefault.TargetObjectId;

        menuArgs.AddMenuItem(trackTargetingParentMenuItem);
    }

    private void OnRegisterPlayerClicked(IMenuItemClickedArgs args)
    {
        if (string.IsNullOrEmpty(targetFullName) || targetObjectId == null)
            return;

        Shared.TargetManager.RegisterPlayer((ulong)targetObjectId, targetFullName);
    }

    private void OnUnregisterPlayerClicked(IMenuItemClickedArgs args)
    {
        if (string.IsNullOrEmpty(targetFullName) || targetObjectId == null)
            return;

        Shared.TargetManager.UnregisterPlayer((ulong)targetObjectId);
    }

    private void OpenTrackTargetingSubmenu(IMenuItemClickedArgs args)
    {
        var submenuItems = new List<MenuItem>();
        submenuItems.Add(registerPlayerMenuItem);
        submenuItems.Add(unregisterPlayerMenuItem);

        args.OpenSubmenu(submenuItems);
    }
}
