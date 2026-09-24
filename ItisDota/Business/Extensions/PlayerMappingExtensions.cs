using WebApplication1.Business.Models;
using WebApplication1.Data.Entities;

namespace WebApplication1.Business.Extensions;

public static class PlayerMappingExtensions
{
    public static PlayerDto ToDto(this Player player)
    {
        return new PlayerDto
        {
            Id = player.Id,
            RealName = player.RealName,
            TgTag = player.TgTag,
            MmRRange = player.MmRRange,
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
