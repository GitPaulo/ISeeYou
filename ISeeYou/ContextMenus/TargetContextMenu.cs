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
            registerPlayerMenuItem = new MenuItem
            {
                Name = "Register Player",
                OnClicked = OnRegisterPlayerClicked
            };

            unregisterPlayerMenuItem = new MenuItem
            {
                Name = "Unregister Player",
                OnClicked = OnUnregisterPlayerClicked
            };

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
            if (menuArgs.Target is not MenuTargetDefault menuTargetDefault)
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
}
