using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace ISeeYou.Windows
{
    public class HistoryWindow : Window, IDisposable
    {
        private int selectedRow = -1;
        private string filterText = string.Empty;
        private bool sortAscending = true;
        private int sortColumn = -1;

        public HistoryWindow() : base("Target History###With a constant ID")
        {
            Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoScrollWithMouse;
            Size = new Vector2(320, 210);
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
            // Filter input
            if (ImGui.InputText("Filter by Name", ref filterText, 100))
            {
                // Optional: Perform actions when the filter text changes
            }

            // Retrieve and process history data
            var history = Shared.TargetManager.TargetHistory;

            // Filter the history
            var filteredHistory = history
                                  .Where(entry => string.IsNullOrEmpty(filterText) ||
                                                  entry.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase))
                                  .ToList();

            // Sort the filtered history if needed
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

            // Begin table container
            ImGui.BeginChild("TableScrollRegion", new Vector2(0, 150), true, ImGuiWindowFlags.HorizontalScrollbar);

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
                    var (_, name, timestamp) = filteredHistory[i];
                    ImGui.TableNextRow();

                    // Name column
                    ImGui.TableSetColumnIndex(0);
                    ImGui.PushID(i);
                    bool isSelected = selectedRow == i;
                    if (ImGui.Selectable(name, isSelected,
                                         ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap))
                    {
                        selectedRow = i;
                        OnRowLeftClick(name);
                    }

                    ImGui.PopID();

                    // Time column
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(timestamp.ToString("HH:mm"));
                }

                ImGui.EndTable();
            }

            ImGui.EndChild();
        }


        private void OnRowLeftClick(string rowData)
        {
            Shared.Log.Debug($"Left-clicked on row: {rowData}");
            // Add your logic for handling left-clicks here
            Shared.Sound.PlaySound(Shared.SoundTargetStartPath);
        }

        private void OnRowRightClick(string rowData)
        {
            Shared.Log.Debug($"Right-clicked on row: {rowData}");
            // Add your logic for handling right-clicks here
        }
    }
}
