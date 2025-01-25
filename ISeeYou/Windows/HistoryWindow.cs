﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace ISeeYou.Windows
{
    public class HistoryWindow : Window, IDisposable
    {
        private string selectedPlayer = null;
        private int selectedRow = -1;
        private string filterText = string.Empty;
        private bool sortAscending = true;
        private int sortColumn = -1;

        public HistoryWindow() : base("Target History###With a constant ID")
        {
            Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoScrollWithMouse;
            Size = new Vector2(500, 300);
            SizeCondition = ImGuiCond.Always;
        }

        public void Dispose() { }

        public override void PreDraw()
        {
            Flags = Shared.Config.IsConfigWindowMovable
                        ? Flags & ~ImGuiWindowFlags.NoMove
                        : Flags | ImGuiWindowFlags.NoMove;
        }

        public override unsafe void Draw()
        {
            var allHistories = Shared.TargetManager.GetAllHistories();
            var playerColors = Shared.TargetManager.GetPlayerColors();

            // 
            // Side List
            //
            ImGui.BeginChild("PlayerList", new Vector2(150, 0), true);
            ImGui.Text("Tracked Players:");
            ImGui.Separator();

            foreach (var (playerId, trackedPlayer) in allHistories)
            {
                var color = playerColors.GetValueOrDefault(playerId, new Vector4(1, 1, 1, 1));
                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(color));

                if (ImGui.Selectable(trackedPlayer.PlayerName, selectedPlayer == trackedPlayer.PlayerName))
                {
                    selectedPlayer = trackedPlayer.PlayerName;
                }

                ImGui.PopStyleColor();
            }

            ImGui.EndChild();
            ImGui.SameLine();

            //
            // History Table
            //
            ImGui.BeginChild("HistoryTableRegion");

            if (selectedPlayer != null)
            {
                var selectedHistory = allHistories.FirstOrDefault(h => h.History.PlayerName == selectedPlayer).History;
                if (selectedHistory != null)
                {
                    DrawTargetHistory(selectedHistory);
                }
                else
                {
                    ImGui.Text("No history available for the selected player.");
                }
            }
            else
            {
                ImGui.Text("Select a player from the list to view their history.");
            }

            ImGui.EndChild();
        }

        private unsafe void DrawTargetHistory(TargetManager.TrackedPlayer trackedPlayer)
        {
            // Filter input
            if (ImGui.InputText("Filter by Name", ref filterText, 100))
            {
                // Optional: Perform actions when the filter text changes
            }

            // Filter and sort the target history
            var filteredHistory = trackedPlayer.TargetHistory
                                               .Where(entry => string.IsNullOrEmpty(filterText) ||
                                                               entry.Name.Contains(
                                                                   filterText, StringComparison.OrdinalIgnoreCase))
                                               .ToList();

            if (sortColumn != -1)
            {
                filteredHistory = sortColumn switch
                {
                    0 => sortAscending
                             ? filteredHistory.OrderBy(entry => entry.Name).ToList()
                             : filteredHistory.OrderByDescending(entry => entry.Name).ToList(),
                    1 => sortAscending
                             ? filteredHistory.OrderBy(entry => entry.Timestamp).ToList()
                             : filteredHistory.OrderByDescending(entry => entry.Timestamp).ToList(),
                    _ => filteredHistory
                };
            }

            // Render table
            if (ImGui.BeginTable("HistoryTable", 2,
                                 ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Sortable |
                                 ImGuiTableFlags.ScrollY))
            {
                // Setup table columns
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultSort);
                ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableHeadersRow();

                // Handle sorting
                var sortSpecsPtr = ImGui.TableGetSortSpecs();
                if (sortSpecsPtr.NativePtr != null && sortSpecsPtr.SpecsDirty)
                {
                    var sortSpec = sortSpecsPtr.Specs;
                    sortColumn = sortSpec.ColumnIndex;
                    sortAscending = sortSpec.SortDirection == ImGuiSortDirection.Ascending;
                    sortSpecsPtr.SpecsDirty = false;
                }

                // Render rows
                for (int i = 0; i < filteredHistory.Count; i++)
                {
                    var (gameObjectId, name, timestamp) = filteredHistory[i];
                    ImGui.TableNextRow();

                    // Name column
                    ImGui.TableSetColumnIndex(0);
                    ImGui.PushID(i);
                    bool isSelected = selectedRow == i;
                    if (ImGui.Selectable(name, isSelected,
                                         ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap))
                    {
                        selectedRow = i;
                        OnRowLeftClick(gameObjectId);
                    }

                    ImGui.PopID();

                    // Time column
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(timestamp.ToString("HH:mm"));
                }

                ImGui.EndTable();
            }
        }

        private void OnRowLeftClick(ulong gameObjectId)
        {
            // Retrieve the game object from the object table
            var gameObject = Shared.ObjectTable.SearchById(gameObjectId);
            if (gameObject != null)
            {
                // Set the local player's target to the selected game object
                Shared.ClientTargetManager.Target = gameObject;
                Shared.Chat.Print($"Targeted player: {gameObject.Name}");
            }
            else
            {
                Shared.Chat.Print("Failed to target player: GameObject not found.");
            }
        }
    }
}
