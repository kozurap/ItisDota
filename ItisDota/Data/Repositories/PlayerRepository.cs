using Microsoft.EntityFrameworkCore;
using ItisDota.Data.Entities;

namespace ItisDota.Data.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly AppDbContext _db;

    public PlayerRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Player?> GetByKeycloakIdAsync(string keycloakUserId, CancellationToken cancellationToken = default)
    {
        return _db.Players.FirstOrDefaultAsync(p => p.KeycloakUserId == keycloakUserId, cancellationToken);
    }

    public Task<bool> IsTgTagTakenAsync(
        string tgTag,
        int? exceptPlayerId = null,
        CancellationToken cancellationToken = default)
    {
        return _db.Players.AnyAsync(
            p => p.TgTag == tgTag && (exceptPlayerId == null || p.Id != exceptPlayerId),
            cancellationToken);
    }

    public async Task<Player> AddAsync(Player player, CancellationToken cancellationToken = default)
    {
        _db.Players.Add(player);
        await _db.SaveChangesAsync(cancellationToken);
        return player;
    }

    public async Task UpdateAsync(Player player, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Players.FirstOrDefaultAsync(p => p.Id == player.Id, cancellationToken)
            ?? throw new InvalidOperationException("Профиль не найден.");

        existing.RealName = player.RealName;
        existing.TgTag = player.TgTag;
        existing.Mmr = player.Mmr;
        existing.RolePriority1 = player.RolePriority1;
        existing.RolePriority2 = player.RolePriority2;
        existing.RolePriority3 = player.RolePriority3;
        existing.RolePriority4 = player.RolePriority4;
        existing.RolePriority5 = player.RolePriority5;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
