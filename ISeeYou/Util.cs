using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using Lumina.Excel.Sheets;

namespace ISeeYou;

public static class Util
{
    public static bool IsLocalPlayerInCombat()
    {
        var player = Shared.ClientState.LocalPlayer;
        return player != null && player.StatusFlags.HasFlag(StatusFlags.InCombat);
    }

    public static bool IsWorldValid(uint worldId)
    {
        return IsWorldValid(GetWorld(worldId));
    }

    public static bool IsWorldValid(World world)
    {
        if (world.Name.IsEmpty || GetRegionCode(world) == string.Empty) return false;

        return char.IsUpper(world.Name.ToString()[0]);
    }

    public static World GetWorld(uint worldId)
    {
        var worldSheet = Shared.DataManager.GetExcelSheet<World>();
        if (!worldSheet.TryGetRow(worldId, out var world)) return worldSheet.First();

        return world;
    }

    public static string GetRegionCode(World world)
    {
        return world.DataCenter.ValueNullable?.Region switch
        {
            1 => "JP",
            2 => "NA",
            3 => "EU",
            4 => "OC",
            _ => string.Empty
        };
    }
}
