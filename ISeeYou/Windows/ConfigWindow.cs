using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace ISeeYou.Windows;

public class ConfigWindow : Window, IDisposable
{
    public ConfigWindow() : base("ISeeYou Configuration###ConfigWindow")
    {
        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        Size = new Vector2(340, 240);
    }

    public void Dispose() { }

    public override void Draw()
    {
        // Is enabled in Combat
        var isEnabledInCombat = Shared.Config.IsEnabledInCombat;
        if (ImGui.Checkbox("Enabled in combat", ref isEnabledInCombat))
        {
            Shared.Config.ShouldPlaySoundOnTarget = isEnabledInCombat;
            Shared.Config.Save();
        }

        // Should play sound on target
        var shouldPlaySoundOnTarget = Shared.Config.ShouldPlaySoundOnTarget;
        if (ImGui.Checkbox("Play sound on target", ref shouldPlaySoundOnTarget))
        {
            Shared.Config.ShouldPlaySoundOnTarget = shouldPlaySoundOnTarget;
            Shared.Config.Save();
        }

        // Should play sound on untarget
        var shouldPlaySoundOnUntarget = Shared.Config.ShouldPlaySoundOnTarget;
        if (ImGui.Checkbox("Play sound on untarget", ref shouldPlaySoundOnUntarget))
        {
            Shared.Config.ShouldPlaySoundOnUntarget = shouldPlaySoundOnUntarget;
            Shared.Config.Save();
        }

        // Should log to chat
        var shouldLogToChat = Shared.Config.ShouldLogToChat;
        if (ImGui.Checkbox("Log to chat", ref shouldLogToChat))
        {
            Shared.Config.ShouldLogToChat = shouldLogToChat;
            Shared.Config.Save();
        }

        // Color picker for LocalPlayerColor
        var localPlayerColor = Shared.Config.LocalPlayerColor;
        if (ImGui.ColorEdit4("Local player color", ref localPlayerColor, ImGuiColorEditFlags.NoInputs))
        {
            Shared.Config.LocalPlayerColor = localPlayerColor;
            Shared.Config.Save();
        }

        ImGui.Text("*Registered players are randomly colored.");

        // Max history entries
        var maxHistoryEntries = Shared.Config.MaxHistoryEntries;
        if (ImGui.InputInt("Max history", ref maxHistoryEntries))
        {
            Shared.Config.MaxHistoryEntries = maxHistoryEntries;
            Shared.Config.Save();
        }

        // Poll frequency
        var pollFrequency = Shared.Config.PollFrequency;
        if (ImGui.InputInt("Poll frequency", ref pollFrequency))
        {
            Shared.Config.PollFrequency = pollFrequency;
            Shared.Config.Save();
        }
    }
}
