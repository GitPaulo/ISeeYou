using Dalamud.Configuration;
using System;

namespace ISeeYou;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool IsConfigWindowMovable { get; set; } = true;
    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;
    
    public int PollFrequency { get; set; } = 100;
    public int MaxHistoryEntries = 100;

    public void Save()
    {
        Shared.PluginInterface.SavePluginConfig(this);
    }
}
