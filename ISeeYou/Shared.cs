using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ISeeYou.Sound;
using ISeeYou.Windows;

namespace ISeeYou;

internal class Shared
{
    public static Configuration Config { get; set; } = null!;
    public static ConfigWindow ConfigWindow { get; set; } = null!;
    public static HistoryWindow HistoryWindow { get; set; } = null!;
    public static TargetManager TargetManager { get; set; } = null!;
    public static SoundEngine Sound { get; set; } = null!;
    
    public static string SoundTargetStartPath { get; set; } = null!;
    public static string SoundTargetStopPath { get; set; } = null!;
    
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IChatGui Chat { get; private set; } = null!;
    [PluginService] internal static INamePlateGui NamePlateGui { get; private set; } = null!;
}
