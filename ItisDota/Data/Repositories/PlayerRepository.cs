using Microsoft.EntityFrameworkCore;
using WebApplication1.Data.Entities;

namespace WebApplication1.Data.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly AppDbContext _db;

    public PlayerRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Player>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Players
            .OrderByDescending(p => p.SelectionCount)
            .ThenBy(p => p.RealName)
            .ToListAsync(cancellationToken);
    }

    public Task<Player?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _db.Players.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task UpsertAsync(Player player, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Players
            .FirstOrDefaultAsync(p => p.TgTag == player.TgTag, cancellationToken);

        if (existing is null)
        {
            _db.Players.Add(player);
            return;
        }

        existing.RealName = player.RealName;
        existing.MmRRange = player.MmRRange;
        existing.RolePriority1 = player.RolePriority1;
        existing.RolePriority2 = player.RolePriority2;
        existing.RolePriority3 = player.RolePriority3;
        existing.RolePriority4 = player.RolePriority4;
        existing.RolePriority5 = player.RolePriority5;
    }

    public async Task UpdateAsync(Player player, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Players.FirstOrDefaultAsync(p => p.Id == player.Id, cancellationToken)
            ?? throw new InvalidOperationException("Игрок не найден.");

        var tagTaken = await _db.Players.AnyAsync(
            p => p.TgTag == player.TgTag && p.Id != player.Id,
            cancellationToken);
        if (tagTaken)
        {
            throw new InvalidOperationException($"Telegram-тег «{player.TgTag}» уже занят другим игроком.");
        }

        existing.RealName = player.RealName;
        existing.TgTag = player.TgTag;
        existing.MmRRange = player.MmRRange;
        existing.RolePriority1 = player.RolePriority1;
        existing.RolePriority2 = player.RolePriority2;
        existing.RolePriority3 = player.RolePriority3;
        existing.RolePriority4 = player.RolePriority4;
        existing.RolePriority5 = player.RolePriority5;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var player = await _db.Players.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (player is not null)
        {
            _db.Players.Remove(player);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task IncrementSelectionCountsAsync(
        IReadOnlyCollection<int> playerIds,
        CancellationToken cancellationToken = default)
    {
        if (playerIds.Count == 0)
        {
            return;
        }

        var players = await _db.Players
            .Where(p => playerIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        foreach (var player in players)
        {
            player.SelectionCount++;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
