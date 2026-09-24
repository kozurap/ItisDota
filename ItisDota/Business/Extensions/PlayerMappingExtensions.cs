using ItisDota.Business.Models;
using ItisDota.Data.Entities;

namespace ItisDota.Business.Extensions;

public static class PlayerMappingExtensions
{
    public static PlayerDto ToDto(this Player player)
    {
        return new PlayerDto
        {
            Id = player.Id,
            RealName = player.RealName,
            TgTag = player.TgTag,
            Mmr = player.Mmr,
            SteamFriendId = player.SteamFriendId,
            RolePriority1 = player.RolePriority1,
            RolePriority2 = player.RolePriority2,
            RolePriority3 = player.RolePriority3,
            RolePriority4 = player.RolePriority4,
            RolePriority5 = player.RolePriority5
        };
    }

    public static IReadOnlyList<PlayerDto> ToDtoList(this IEnumerable<Player> players)
    {
        return players.Select(p => p.ToDto()).ToList();
    }
}
