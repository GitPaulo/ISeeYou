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
    private readonly Stopwatch updateTimer = new();
    private IPlayerCharacter[] currentTargetingPlayers = Array.Empty<IPlayerCharacter>();
    private readonly List<IPlayerCharacter> targetHistory = new();

    public IReadOnlyCollection<IPlayerCharacter> CurrentTargetingPlayers => currentTargetingPlayers;
    public IReadOnlyCollection<IPlayerCharacter> TargetHistory => targetHistory;

    public TargetManager()
    {
        updateTimer.Start();
        Shared.Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        Shared.Framework.Update -= OnFrameworkUpdate;
    }

    public void ClearTargetHistory() => targetHistory.Clear();

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
        var stoppedTargeting = currentTargetingPlayers.Except(newlyTargetingPlayers, new PlayerCharacterComparer());
        var startedTargeting = newlyTargetingPlayers.Except(currentTargetingPlayers, new PlayerCharacterComparer());

        foreach (var player in startedTargeting)
        {
            Shared.Log.Information($"Started targeting: {player.Name}");
        }

        foreach (var player in stoppedTargeting)
        {
            Shared.Log.Information($"Stopped targeting: {player.Name}");
        }
    }

    private void UpdateTargetHistory(IPlayerCharacter[] newlyTargetingPlayers)
    {
        foreach (var player in newlyTargetingPlayers)
        {
            targetHistory.RemoveAll(t => t.GameObjectId == player.GameObjectId);
            targetHistory.Insert(0, player);
        }

        while (targetHistory.Count > Shared.Config.MaxHistoryEntries)
            targetHistory.RemoveAt(targetHistory.Count - 1);
    }

    private class PlayerCharacterComparer : IEqualityComparer<IPlayerCharacter>
    {
        public bool Equals(IPlayerCharacter? x, IPlayerCharacter? y) => x?.GameObjectId == y?.GameObjectId;

        public int GetHashCode(IPlayerCharacter obj) => obj.GameObjectId.GetHashCode();
    }
}
