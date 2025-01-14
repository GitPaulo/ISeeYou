using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using ISeeYou.Windows;

namespace ISeeYou;

public sealed class Plugin : IDalamudPlugin
{
    private const string Name = "ISeeYou";
    private const string CommandName = "/icu";

    private readonly WindowSystem windowSystem = new(Name);

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Shared>();

        Shared.Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        InitWindows();
        InitCommands();
        InitHooks();

        Shared.Log.Information($"Loaded {Shared.PluginInterface.Manifest.Name}");
    }

    private void InitWindows()
    {
        Shared.ConfigWindow = new ConfigWindow();
        Shared.TargetManager = new TargetManager();

        windowSystem.AddWindow(Shared.ConfigWindow);
    }

    private void InitCommands()
    {
        Shared.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Use /icu to toggle the main window or /icu debug to test TargetManager"
        });
    }

    private void InitHooks()
    {
        Shared.PluginInterface.UiBuilder.Draw += DrawUI;
        Shared.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
    }

    public void Dispose()
    {
        windowSystem.RemoveAllWindows();

        Shared.ConfigWindow.Dispose();
        Shared.TargetManager.Dispose();

        Shared.CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        Shared.Log.Information("Command received: " + command + " " + args);
        DebugTargetManager();
    }

    private void DebugTargetManager()
    {
        var currentTargetingPlayers = Shared.TargetManager.CurrentTargetingPlayers;
        var targetHistory = Shared.TargetManager.TargetHistory;

        // Log current targeters
        if (currentTargetingPlayers.Count > 0)
        {
            Shared.Log.Information("Current Targeting Players:");
            foreach (var player in currentTargetingPlayers)
            {
                Shared.Log.Information($"- {player.Name}");
            }
        }
        else
        {
            Shared.Log.Information("No players are currently targeting you.");
        }

        // Log target history
        if (targetHistory.Count > 0)
        {
            Shared.Log.Information("Target History:");
            foreach (var player in targetHistory)
            {
                Shared.Log.Information($"- {player.Name}");
            }
        }
        else
        {
            Shared.Log.Information("No target history available.");
        }
    }

    private void DrawUI() => windowSystem.Draw();
    public void ToggleConfigUI() => Shared.ConfigWindow.Toggle();
}
