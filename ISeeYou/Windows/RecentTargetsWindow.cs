using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace ISeeYou.Windows;

public class RecentTargetsWindow : Window, IDisposable
{
    private ulong lastFocusedTarget;

    public RecentTargetsWindow() : base("ISeeYou Me ###RecentTargets")
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
        var hasTargets = localPlayerEntry.History != null && localPlayerEntry.History.TargetHistory.Any();
        var currentlyTargetedPlayers = localPlayerEntry.History.CurrentTargetingPlayers
                                                       .Select(p => p.GameObjectId)
                                                       .ToHashSet();
        if (!hasTargets)
        {
            ImGui.Text("No recent targets.");
            return;
        }

        // Filter history to only keep the latest entry per player
        var uniqueTargets = localPlayerEntry.History.TargetHistory
                                            .GroupBy(entry => entry.Name) // Group by player name
                                            .Select(group => group.OrderByDescending(e => e.Timestamp)
                                                                  .First()) // Get latest entry
                                            .ToList();

        ImGui.BeginChild("RecentTargetsList", new Vector2(0, 0), true);
        var anyHovered = false; // Track if any item is hovered

        foreach (var (gameObjectId, name, timestamp) in uniqueTargets)
        {
            var isCurrentTarget = currentlyTargetedPlayers.Contains(gameObjectId);

            var textColor = isCurrentTarget
                                ? new Vector4(0.0f, 0.8f, 1.0f, 1.0f)  // Light blue for current target
                                : new Vector4(0.6f, 0.6f, 0.6f, 1.0f); // Default faded grey

            ImGui.PushStyleColor(ImGuiCol.Text, textColor);

            if (ImGui.Selectable($"{name} ({timestamp:hh:mm tt})", false, ImGuiSelectableFlags.DontClosePopups))
                TargetPlayer(gameObjectId);

            var isHovered = ImGui.IsItemHovered();
            ImGui.PopStyleColor();

            if (isHovered)
            {
                SetFocusTarget(gameObjectId);
                anyHovered = true;
            }
        }

        ImGui.EndChild();

        // If nothing is hovered, clear focus target
        if (!anyHovered && lastFocusedTarget != 0) ClearFocusTarget();
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
            Shared.Chat.Print("Failed to target: Player not found.");
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
