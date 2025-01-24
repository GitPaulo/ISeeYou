namespace ISeeYou;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

public class TargetManager : IDisposable
{
    private IPlayerCharacter[] currentTargetingPlayers = Array.Empty<IPlayerCharacter>();
    
    private readonly List<(ulong GameObjectId, string Name, DateTime Timestamp)> targetHistory = new();
    private readonly Stopwatch updateTimer = new();
    
    public IReadOnlyCollection<IPlayerCharacter> CurrentTargetingPlayers => currentTargetingPlayers;
    public IReadOnlyCollection<(ulong GameObjectId, string Name, DateTime Timestamp)> TargetHistory => targetHistory;
    public void ClearTargetHistory() => targetHistory.Clear();

    public TargetManager()
    {
        InitUpdateTimer();
    }
    
    public void Dispose()
    {
        Shared.Framework.Update -= OnFrameworkUpdate;
    }
    
    private void InitUpdateTimer()
    {
        updateTimer.Start();
        Shared.Framework.Update += OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (updateTimer.ElapsedMilliseconds <= Shared.Config.PollFrequency)
            return;

        updateTimer.Restart();
        UpdateTargetingPlayers();
    }

    private void UpdateTargetingPlayers()
    {
        var localPlayer = Shared.ClientState.LocalPlayer;
        if (localPlayer == null)
            return;

        var newlyTargetingPlayers = GetPlayersTargetingLocalPlayer(Shared.ObjectTable, localPlayer);

        LogTargetingChanges(newlyTargetingPlayers);
        UpdateTargetHistory(newlyTargetingPlayers);

        currentTargetingPlayers = newlyTargetingPlayers;
    }

    private IPlayerCharacter[] GetPlayersTargetingLocalPlayer(IEnumerable<IGameObject> objects, IGameObject localPlayer)
    {
        return objects
            .OfType<IPlayerCharacter>()
            .Where(obj => obj.TargetObjectId == localPlayer.GameObjectId)
            .ToArray();
    }

    private void LogTargetingChanges(IPlayerCharacter[] newlyTargetingPlayers)
    {
        var stoppedTargeting = currentTargetingPlayers.Except(newlyTargetingPlayers, new PlayerCharacterComparer()).ToList();
        var startedTargeting = newlyTargetingPlayers.Except(currentTargetingPlayers, new PlayerCharacterComparer()).ToList();

        if (startedTargeting.Count != 0)
        {
            var names = string.Join(", ", startedTargeting.Select(player => player.Name));
            var message = startedTargeting.Count == 1
                              ? $"{names} is targeting you."
                              : $"{names} are targeting you.";
            Shared.Chat.Print(message);
            Shared.Sound.PlaySound(Shared.SoundTargetStartPath);
        }

        if (stoppedTargeting.Count != 0)
        {
            var names = string.Join(", ", stoppedTargeting.Select(player => player.Name));
            var message = stoppedTargeting.Count == 1
                              ? $"{names} stopped targeting you."
                              : $"{names} have stopped targeting you.";
            Shared.Chat.Print(message);
            Shared.Sound.PlaySound(Shared.SoundTargetStopPath);
        }
    }
    
    private void UpdateTargetHistory(IPlayerCharacter[] newlyTargetingPlayers)
    {
        foreach (IPlayerCharacter player in newlyTargetingPlayers)
        {
            // Use immutable data: GameObjectId and Name
            targetHistory.RemoveAll(t => t.GameObjectId == player.GameObjectId);

            targetHistory.Insert(0, (player.GameObjectId, player.Name.TextValue, DateTime.Now));
            Shared.Log.Debug($"Inserted {player.Name.TextValue} into target history with timestamp {DateTime.Now:HH:mm}.");
        }

        // Maintain history size
        while (targetHistory.Count > Shared.Config.MaxHistoryEntries)
            targetHistory.RemoveAt(targetHistory.Count - 1);
    }

    private class PlayerCharacterComparer : IEqualityComparer<IPlayerCharacter>
    {
        public bool Equals(IPlayerCharacter? x, IPlayerCharacter? y) => x?.GameObjectId == y?.GameObjectId;

        public int GetHashCode(IPlayerCharacter obj) => obj.GameObjectId.GetHashCode();
    }
}
