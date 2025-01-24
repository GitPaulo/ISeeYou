using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using ISeeYou.Sound;
using ISeeYou.Windows;

namespace ISeeYou;

public sealed class Plugin : IDalamudPlugin
{
    private const string Name = "ISeeYou";
    private const string CommandName = "/icu";
    private const string CommandNameClear = "/icuc";

    private readonly WindowSystem windowSystem = new(Name);

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Shared>();

        Shared.Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        InitWindows();
        InitCommands();
        InitResources();
        InitServices();
        InitHooks();

        Shared.Log.Information($"Loaded {Shared.PluginInterface.Manifest.Name}");
    }

    private void InitWindows()
    {
        Shared.ConfigWindow = new ConfigWindow();
        Shared.HistoryWindow = new HistoryWindow();

        windowSystem.AddWindow(Shared.ConfigWindow);
        windowSystem.AddWindow(Shared.HistoryWindow);
    }

    private void InitCommands()
    {
        Shared.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Use /icu to print target history and use main window"
        });
        
        Shared.CommandManager.AddHandler(CommandNameClear, new CommandInfo(OnCommandClear)
        {
            HelpMessage = "Use /icuc to clear target history"
        });
    }

    private void InitResources()
    { 
        var assemblyDirectory = Shared.PluginInterface.AssemblyLocation.Directory?.FullName!;
        
        Shared.SoundTargetStartPath = Path.Combine(assemblyDirectory, "target_start.mp3");
        Shared.SoundTargetStopPath = Path.Combine(assemblyDirectory, "target_stop.mp3");
    }
    
    private void InitServices()
    {
        Shared.TargetManager = new TargetManager();
        Shared.Sound = new SoundEngine();
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
        Shared.Log.Debug("Toggling main window");
        ToggleHistoryUI();       
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
            foreach (var (gameObjectId, name, timestamp) in targetHistory)
            {
                Shared.Chat.Print($"- {name} (ID: {gameObjectId}) at {timestamp:HH:mm}");
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
    public void ToggleHistoryUI() => Shared.HistoryWindow.Toggle();
}
