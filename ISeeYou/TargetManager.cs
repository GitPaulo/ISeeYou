﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

namespace ISeeYou;

public class TargetManager : IDisposable
{
    private readonly Dictionary<ulong, Vector4> playerColors = new();
    private readonly Dictionary<ulong, TrackedPlayer> trackedPlayers = new();
    private readonly Stopwatch updateTimer = new();

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

        if (Util.IsLocalPlayerInCombat() && !Shared.Config.IsEnabledInCombat)
            return;

        UpdateAllTrackedPlayers();
    }

    public void RegisterPlayer(ulong playerId, string playerName, Vector4? colorOverride = null)
    {
        if (trackedPlayers.ContainsKey(playerId))
        {
            Shared.Chat.Print($"Player {playerName} (ID: {playerId}) is already being tracked.");
            return;
        }

        trackedPlayers[playerId] = new TrackedPlayer(playerId, playerName);
        // Use the provided color override or generate a bright color
        playerColors[playerId] = colorOverride ?? GenerateBrightColor();

        Shared.Log.Debug($"Registered player {playerName} (ID: {playerId}) with color {playerColors[playerId]}.");

        if (Shared.Config.ShouldLogToChat)
        {
            Shared.Chat.Print(
                $"Registered player {playerName} (ID: {playerId}) with color {playerColors[playerId]}.");
        }
    }

    public void UnregisterPlayer(ulong playerId)
    {
        if (trackedPlayers.Remove(playerId))
        {
            playerColors.Remove(playerId); // Remove associated color
            Shared.Log.Debug($"Unregistered player (ID: {playerId}) from tracking.");

            if (Shared.Config.ShouldLogToChat)
                Shared.Chat.Print($"Unregistered player (ID: {playerId}) from tracking.");
        }
    }

    public void UpdatePlayerColor(ulong playerId, Vector4 color)
    {
        if (playerColors.ContainsKey(playerId))
        {
            playerColors[playerId] = color;
            Shared.Log.Debug($"Updated color for player (ID: {playerId}) to {color}.");
        }
    }

    public IReadOnlyDictionary<ulong, Vector4> GetPlayerColors()
    {
        return playerColors;
        // Expose colors publicly
    }

    private Vector4 GenerateBrightColor()
    {
        // Generate a random bright color
        var random = new Random();
        var r = random.Next(128, 256) / 255f; // Scale to 0.0 - 1.0 for Vector4
        var g = random.Next(128, 256) / 255f;
        var b = random.Next(128, 256) / 255f;
        var a = 1.0f; // Full opacity

        return new Vector4(r, g, b, a);
    }

    public IReadOnlyCollection<(ulong PlayerId, TrackedPlayer History)> GetAllHistories()
    {
        return trackedPlayers.Select(kv => (kv.Key, kv.Value)).ToList();
    }

    private void UpdateAllTrackedPlayers()
    {
        var objects = Shared.ObjectTable.ToArray();
        foreach (var player in trackedPlayers.Values) player.Update(objects);
    }


    public void ForceSoftRefresh()
    {
        Shared.NamePlateGui.RequestRedraw();
    }

    public class TrackedPlayer(ulong playerId, string playerName)
    {
        private readonly List<(ulong GameObjectId, string Name, DateTime Timestamp)> targetHistory = new();

        private IPlayerCharacter[] currentTargetingPlayers = Array.Empty<IPlayerCharacter>();
        private IPlayerCharacter[] previousTargetingPlayers = Array.Empty<IPlayerCharacter>(); // Track previous state
        public ulong PlayerId { get; } = playerId;
        public string PlayerName { get; } = playerName;

        public IReadOnlyCollection<IPlayerCharacter> CurrentTargetingPlayers => currentTargetingPlayers;

        public IReadOnlyCollection<(ulong GameObjectId, string Name, DateTime Timestamp)> TargetHistory =>
            targetHistory;

        public void Update(IEnumerable<IGameObject> objects)
        {
            var registeredPlayer =
                Shared.ObjectTable.FirstOrDefault(obj => obj.GameObjectId == PlayerId) as IPlayerCharacter;
            if (registeredPlayer == null) return;

            var newlyTargetingPlayers = objects
                                        .OfType<IPlayerCharacter>()
                                        .Where(obj => obj.TargetObjectId == registeredPlayer.GameObjectId)
                                        .ToArray();

            previousTargetingPlayers = currentTargetingPlayers;
            UpdateTargetHistory(newlyTargetingPlayers);
            currentTargetingPlayers = newlyTargetingPlayers;
        }

        private void UpdateTargetHistory(IPlayerCharacter[] newlyTargetingPlayers)
        {
            // Players who have started targeting
            var startedTargeting = newlyTargetingPlayers
                                   .Where(newPlayer =>
                                              previousTargetingPlayers.All(
                                                  prevPlayer => prevPlayer.GameObjectId != newPlayer.GameObjectId))
                                   .ToList();

            // Players who have stopped targeting
            var stoppedTargeting = previousTargetingPlayers
                                   .Where(prevPlayer =>
                                              newlyTargetingPlayers.All(
                                                  newPlayer => newPlayer.GameObjectId != prevPlayer.GameObjectId))
                                   .ToList();

            // Log players who have started targeting
            foreach (var player in startedTargeting)
            {
                targetHistory.Insert(0, (player.GameObjectId, player.Name.TextValue, DateTime.Now));

                if (Shared.Config.ShouldPlaySoundOnTarget) Shared.Sound.PlaySound(Shared.SoundTargetStartPath);

                if (Shared.Config.ShouldLogToChat)
                {
                    Shared.Chat.Print($"{player.Name.TextValue} started targeting {
                        (playerId == Shared.ClientState.LocalPlayer!.GameObjectId ? "YOU" : playerName)
                    } at {DateTime.Now:HH:mm}.");
                }

                Shared.TargetManager.ForceSoftRefresh();
            }

            // Log players who have stopped targeting
            foreach (var player in stoppedTargeting)
            {
                if (Shared.Config.ShouldPlaySoundOnUntarget) Shared.Sound.PlaySound(Shared.SoundTargetStopPath);

                // chat print stopped and started targeting
                Shared.Log.Debug($"Stopped Targeting: {player.Name.TextValue} - {player.GameObjectId}");
                if (Shared.Config.ShouldLogToChat)
                {
                    Shared.Chat.Print($"{player.Name.TextValue} stopped targeting {
                        (playerId == Shared.ClientState.LocalPlayer!.GameObjectId ? "YOU" : playerName)
                    } at {DateTime.Now:HH:mm}.");
                }

                Shared.TargetManager.ForceSoftRefresh();
            }

            // Maintain history size
            while (targetHistory.Count > Shared.Config.MaxHistoryEntries)
                targetHistory.RemoveAt(targetHistory.Count - 1);
        }

        public void ClearTargetHistory()
        {
            targetHistory.Clear();
            Shared.Log.Debug($"Cleared target history for {PlayerName} (ID: {PlayerId}).");
        }
    }
}
