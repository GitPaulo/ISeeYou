using System.Collections.Generic;
using System.IO;
using Dalamud.Game.Command;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ISeeYou.ContextMenus;
using ISeeYou.Sound;
using ISeeYou.Windows;

namespace ISeeYou;

public sealed class Plugin : IDalamudPlugin
{
    private const string Name = "ISeeYou";
    private const string CommandName = "/icu";
    private const string CommandNameClear = "/icuc";

    private TargetContextMenu targetContextMenu = null!;
    private readonly WindowSystem windowSystem = new(Name);

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Shared>();

        Shared.Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        InitWindows();
        InitContextMenu();
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

    private void InitContextMenu()
    {
        targetContextMenu = new TargetContextMenu();
        targetContextMenu.Enable();
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

        // Register the local player for tracking
        var localPlayer = Shared.ClientState.LocalPlayer;
        if (localPlayer != null)
        {
            Shared.TargetManager.RegisterPlayer(localPlayer.GameObjectId, localPlayer.Name.TextValue,
                                                Shared.Config.LocalPlayerColor);
        }
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
        targetContextMenu.Disable();

        Shared.ConfigWindow.Dispose();
        Shared.TargetManager.Dispose();

        Shared.CommandManager.RemoveHandler(CommandName);
        Shared.CommandManager.RemoveHandler(CommandNameClear);

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
        var allHistories = Shared.TargetManager.GetAllHistories();

        foreach (var (playerId, trackedPlayer) in allHistories)
        {
            trackedPlayer.ClearTargetHistory();
            Shared.Chat.Print($"Cleared target history for {trackedPlayer.PlayerName} (ID: {playerId}).");
        }
    }

    private void ChatPrintTargetHistory()
    {
        var allHistories = Shared.TargetManager.GetAllHistories();

        // Log target history for each tracked player
        if (allHistories.Count > 0)
        {
            foreach (var (playerId, trackedPlayer) in allHistories)
            {
                Shared.Chat.Print($"Target History for {trackedPlayer.PlayerName} (ID: {playerId}):");

                if (trackedPlayer.TargetHistory.Count > 0)
                {
                    foreach (var (gameObjectId, name, timestamp) in trackedPlayer.TargetHistory)
                    {
                        Shared.Chat.Print($"- {name} (ID: {gameObjectId}) at {timestamp:HH:mm}");
                    }
                }
                else
                {
                    Shared.Chat.Print("  No targets in history.");
                }
            }
        }
        else
        {
            Shared.Chat.Print("No players are currently being tracked.");
        }
    }

    private void NamePlateGui_OnNamePlateUpdate(
        INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        var trackedPlayers = Shared.TargetManager.GetAllHistories();
        var playerColors = Shared.TargetManager.GetPlayerColors();

        // TODO: Very innefficient, should be optimized
        // Create a mapping of targeting players to registered players
        var targetingToRegisteredMap = new Dictionary<ulong, ulong>();
        foreach (var (registeredPlayerId, trackedPlayer) in trackedPlayers)
        {
            foreach (var targetingPlayer in trackedPlayer.CurrentTargetingPlayers)
            {
                targetingToRegisteredMap.TryAdd(targetingPlayer.GameObjectId, registeredPlayerId);
            }
        }

        foreach (var handler in handlers)
        {
            if (handler.NamePlateKind != NamePlateKind.PlayerCharacter)
                continue;

            var playerCharacter = handler.PlayerCharacter;
            if (playerCharacter == null || !playerCharacter.IsValid())
                continue;

            // Check if this player is targeting a registered player
            if (targetingToRegisteredMap.TryGetValue(playerCharacter.GameObjectId, out var registeredPlayerId))
            {
                // Retrieve the color associated with the registered player
                if (playerColors.TryGetValue(registeredPlayerId, out var color))
                {
                    var colorUint = ImGui.ColorConvertFloat4ToU32(color);
                    handler.EdgeColor = colorUint;
                    handler.TextColor = colorUint;

                    Shared.Log.Debug($"Highlighting {playerCharacter.Name.TextValue} with color {color}.");
                }
            }
        }
    }

    private void DrawUI() => windowSystem.Draw();
    public void ToggleConfigUI() => Shared.ConfigWindow.Toggle();
    public void ToggleHistoryUI() => Shared.HistoryWindow.Toggle();
}
