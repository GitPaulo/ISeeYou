using System.Collections.Generic;
using Dalamud.Game.Gui.ContextMenu;

namespace ISeeYou.ContextMenus
{
    public class TargetContextMenu
    {
        private readonly MenuItem registerPlayerMenuItem;
        private readonly MenuItem unregisterPlayerMenuItem;
        private readonly MenuItem trackTargetingParentMenuItem;
        private string? targetFullName;
        private ulong? targetObjectId;

        public TargetContextMenu()
        {
            // Initialize the "Register Player" menu item
            registerPlayerMenuItem = new MenuItem
            {
                Name = "Register Player",
                OnClicked = OnRegisterPlayerClicked
            };

            // Initialize the "Unregister Player" menu item
            unregisterPlayerMenuItem = new MenuItem
            {
                Name = "Unregister Player",
                OnClicked = OnUnregisterPlayerClicked
            };

            // Initialize the parent menu item "Track Targeting" with child items
            trackTargetingParentMenuItem = new MenuItem
            {
                Name = "Track Targeting",
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

        private void OnContextMenuOpened(IMenuOpenedArgs menuArgs)
        {
            // Ensure the context menu is for a player
            if (menuArgs.Target is not MenuTargetDefault menuTargetDefault)
                return;

            // Retrieve the target's full name and object ID
            targetFullName = menuTargetDefault.TargetName;
            targetObjectId = menuTargetDefault.TargetObjectId;

            // Add the "Track Targeting" parent menu item with its children
            menuArgs.AddMenuItem(trackTargetingParentMenuItem);
        }

        private void OnRegisterPlayerClicked(IMenuItemClickedArgs args)
        {
            if (string.IsNullOrEmpty(targetFullName) || targetObjectId == null)
                return;

            // Register the player with the TargetManager
            Shared.TargetManager.RegisterPlayer((ulong)targetObjectId, targetFullName);

            // Optional: Send feedback to the chat
            Shared.Chat.Print($"Registered player: {targetFullName} (ID: {targetObjectId}).");
        }

        private void OnUnregisterPlayerClicked(IMenuItemClickedArgs args)
        {
            if (string.IsNullOrEmpty(targetFullName) || targetObjectId == null)
                return;

            // Unregister the player with the TargetManager
            Shared.TargetManager.UnregisterPlayer((ulong)targetObjectId);

            // Optional: Send feedback to the chat
            Shared.Chat.Print($"Unregistered player: {targetFullName} (ID: {targetObjectId}).");
        }
        
        private void OpenTrackTargetingSubmenu(IMenuItemClickedArgs args)
        {
            var submenuItems = new List<MenuItem>();
            submenuItems.Add(registerPlayerMenuItem);
            submenuItems.Add(unregisterPlayerMenuItem);
            
            args.OpenSubmenu(submenuItems);
        }
    }
}
