using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace ISeeYou.Windows
{
    public class RecentTargetsWindow : Window, IDisposable
    {
        private ulong lastFocusedTarget = 0;

        public RecentTargetsWindow() : base("ISeeYOUR Targets###RecentTargets")
        {
            Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
            Size = new Vector2(250, 300);
            SizeCondition = ImGuiCond.FirstUseEver;
        }

        public void Dispose()
        {
            ClearFocusTarget();
        }

        public override void Draw()
        {
            var localPlayer = Shared.ClientState.LocalPlayer;
            if (localPlayer == null) return;

            var localPlayerEntry = Shared.TargetManager.GetAllHistories()
                                         .FirstOrDefault(h => h.PlayerId == localPlayer.GameObjectId);

            if (localPlayerEntry.History == null || !localPlayerEntry.History.TargetHistory.Any())
            {
                ImGui.Text("No recent targets.");
                return;
            }

            var currentlyTargetedPlayers = localPlayerEntry.History.CurrentTargetingPlayers
                                                           .Select(p => p.GameObjectId)
                                                           .ToHashSet();

            ImGui.BeginChild("RecentTargetsList", new Vector2(0, 0), true);
            bool anyHovered = false; // Track if any item is hovered

            foreach (var (gameObjectId, name, timestamp) in localPlayerEntry.History.TargetHistory)
            {
                bool isCurrentTarget = currentlyTargetedPlayers.Contains(gameObjectId);
                bool isHovered = ImGui.IsItemHovered();

                Vector4 textColor = isCurrentTarget
                                        ? new Vector4(0.0f, 0.8f, 1.0f, 1.0f) // Light blue for current target
                                        : isHovered
                                            ? new Vector4(1.0f, 1.0f, 1.0f, 1.0f)  // Bright white on hover
                                            : new Vector4(0.6f, 0.6f, 0.6f, 1.0f); // Default faded grey

                ImGui.PushStyleColor(ImGuiCol.Text, textColor);

                if (ImGui.Selectable($"{name} ({timestamp:hh:mm tt})", false, ImGuiSelectableFlags.DontClosePopups))
                {
                    TargetPlayer(gameObjectId);
                }

                ImGui.PopStyleColor();

                if (isHovered)
                {
                    SetFocusTarget(gameObjectId);
                    anyHovered = true;
                }
            }

            ImGui.EndChild();

            // If nothing is hovered, clear focus target
            if (!anyHovered && lastFocusedTarget != 0)
            {
                ClearFocusTarget();
            }
        }

        private void TargetPlayer(ulong gameObjectId)
        {
            var gameObject = Shared.ObjectTable.SearchById(gameObjectId);
            if (gameObject != null)
            {
                Shared.ClientTargetManager.Target = gameObject;
                Shared.Chat.Print($"Targeted: {gameObject.Name}");
            }
            else
            {
                Shared.Chat.Print("Failed to target: Player not found.");
            }
        }

        private void SetFocusTarget(ulong gameObjectId)
        {
            if (lastFocusedTarget == gameObjectId) return; // Avoid redundant calls

            var gameObject = Shared.ObjectTable.SearchById(gameObjectId);
            if (gameObject != null)
            {
                Shared.ClientTargetManager.FocusTarget = gameObject;
                lastFocusedTarget = gameObjectId;
            }
        }

        private void ClearFocusTarget()
        {
            Shared.ClientTargetManager.FocusTarget = null;
            lastFocusedTarget = 0;
        }
    }
}
