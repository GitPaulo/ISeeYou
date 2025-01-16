using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using ISeeYou.Windows;

namespace ISeeYou;

public sealed class Plugin : IDalamudPlugin
{
    private const string Name = "ISeeYou";
    private const string CommandName = "/icu";
    private const string CommandNameClear = "/icu";

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
        
        Shared.CommandManager.AddHandler(CommandNameClear, new CommandInfo(OnCommandClear)
        {
            HelpMessage = "Use /icuc to clear target history"
        });
    }

    private void InitHooks()
    {
        Shared.PluginInterface.UiBuilder.Draw += DrawUI;
        Shared.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        
        Shared.NamePlateGui.OnNamePlateUpdate += NamePlateGui_OnNamePlateUpdate;
    }

    public void Dispose()
    {
        windowSystem.RemoveAllWindows();

        Shared.ConfigWindow.Dispose();
        Shared.TargetManager.Dispose();

        Shared.CommandManager.RemoveHandler(CommandName);
        
        Shared.NamePlateGui.OnNamePlateUpdate -= NamePlateGui_OnNamePlateUpdate;
    }

    private void OnCommand(string command, string args)
    {
        ChatPrintTargetHistory();
    }
    
    private void OnCommandClear(string command, string args)
    {
        Shared.TargetManager.ClearTargetHistory();
        Shared.Chat.Print("Target history cleared.");
    }

    private void ChatPrintTargetHistory()
    {
        var currentTargetingPlayers = Shared.TargetManager.CurrentTargetingPlayers;
        var targetHistory = Shared.TargetManager.TargetHistory;

        // Log current targeters
        if (currentTargetingPlayers.Count > 0)
        {
            Shared.Chat.Print("Current Targeting Players:");
            foreach (var player in currentTargetingPlayers)
            {
                Shared.Chat.Print($"- {player.Name}");
            }
        }
        else
        {
            Shared.Chat.Print("No players are currently targeting you.");
        }

        // Log target history
        if (targetHistory.Count > 0)
        {
            Shared.Chat.Print("Target History:");
            foreach (var player in targetHistory)
            { 
                Shared.Chat.Print($"- {player.Name}");
            }
        }
        else
        {
            Shared.Chat.Print("No target history available.");
        }
    }
    
    private void NamePlateGui_OnNamePlateUpdate(
        INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        var currentTargetingPlayers = Shared.TargetManager.CurrentTargetingPlayers;
        // Highlight nameplates of players targeting you
        foreach (var handler in handlers)
        {
            // Skip non-player nameplates
            if (handler.NamePlateKind != NamePlateKind.PlayerCharacter) continue;
    
            // Skip invalid player characters
            var playerCharacter = handler.PlayerCharacter;
            if (playerCharacter == null || !playerCharacter.IsValid()) continue;

            if (currentTargetingPlayers.Contains(playerCharacter))
            {
                Shared.Log.Debug($"Highlighting nameplate for {playerCharacter.Name.TextValue}");
                // uint yellow bright
                handler.EdgeColor = 0xFFFF00FF;
                handler.TextColor = 0xFFFF00FF;
            }
        }
    }

    private void DrawUI() => windowSystem.Draw();
    public void ToggleConfigUI() => Shared.ConfigWindow.Toggle();
}
