using Dalamud.Configuration;
using System;
using System.Numerics;

namespace ISeeYou;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    
    public bool IsEnabledInCombat { get; set; } = true;
    public bool ShouldPlaySoundOnTarget { get; set; } = true;
    public bool ShouldPlaySoundOnUntarget { get; set; } = true;
    public bool ShouldLogToChat { get; set; } = true;
    public Vector4 LocalPlayerColor { get; set; } = new(1, 1, 1, 1);
    
    public int PollFrequency { get; set; } = 100;
    public int MaxHistoryEntries = 1000;

    public void Save()
    {
        Shared.PluginInterface.SavePluginConfig(this);
        
        // Update local player color in TargetManager
        var localPlayer = Shared.ClientState.LocalPlayer;
        if (localPlayer != null && localPlayer.IsValid()) {
            Shared.TargetManager.UpdatePlayerColor(localPlayer.GameObjectId, LocalPlayerColor);
        }
    }
}
