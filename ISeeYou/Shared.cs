using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ISeeYou.Windows;

namespace ISeeYou;

internal class Shared
{
    public static Configuration Config { get; set; } = null!;
    public static ConfigWindow ConfigWindow { get; set; } = null!;
    public static TargetManager TargetManager { get; set; } = null!;
    
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
}
