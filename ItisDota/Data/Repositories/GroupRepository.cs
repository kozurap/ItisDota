using Microsoft.EntityFrameworkCore;
using ItisDota.Data.Entities;

namespace ItisDota.Data.Repositories;

public class GroupRepository : IGroupRepository
{
    private readonly AppDbContext _db;

    public GroupRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PlayerGroup>> GetGroupsForPlayerAsync(
        int playerId,
        CancellationToken cancellationToken = default)
    {
        return await _db.PlayerGroups
            .AsNoTracking()
            .Where(g => g.Members.Any(m => m.PlayerId == playerId))
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<PlayerGroup?> GetByIdAsync(int groupId, CancellationToken cancellationToken = default)
    {
        return _db.PlayerGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
    }

    public Task<PlayerGroup?> GetByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken = default)
    {
        return _db.PlayerGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.InviteToken == inviteToken, cancellationToken);
    }

    public async Task<PlayerGroup> AddAsync(PlayerGroup group, CancellationToken cancellationToken = default)
    {
        _db.PlayerGroups.Add(group);
        await _db.SaveChangesAsync(cancellationToken);
        return group;
    }

    public Task<bool> IsMemberAsync(int groupId, int playerId, CancellationToken cancellationToken = default)
    {
        return _db.GroupMembers.AnyAsync(
            m => m.GroupId == groupId && m.PlayerId == playerId,
            cancellationToken);
    }

    public async Task AddMemberAsync(int groupId, int playerId, CancellationToken cancellationToken = default)
    {
        _db.GroupMembers.Add(new GroupMember
        {
            GroupId = groupId,
            PlayerId = playerId,
            JoinedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GroupMember>> GetMembersAsync(
        int groupId,
        CancellationToken cancellationToken = default)
    {
        return await _db.GroupMembers
            .AsNoTracking()
            .Include(m => m.Player)
            .Where(m => m.GroupId == groupId)
            .OrderByDescending(m => m.SelectionCount)
            .ThenBy(m => m.Player!.RealName)
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetMemberCountAsync(int groupId, CancellationToken cancellationToken = default)
    {
        return _db.GroupMembers.CountAsync(m => m.GroupId == groupId, cancellationToken);
    }

    public async Task SavePromptAsync(
        int groupId,
        string? promptText,
        CancellationToken cancellationToken = default)
    {
        var group = await _db.PlayerGroups.FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken)
            ?? throw new InvalidOperationException("Группа не найдена.");

        group.PromptText = promptText;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task IncrementSelectionCountsAsync(
        int groupId,
        IReadOnlyCollection<int> playerIds,
        CancellationToken cancellationToken = default)
    {
        if (playerIds.Count == 0)
        {
            return;
        }

        var members = await _db.GroupMembers
            .Where(m => m.GroupId == groupId && playerIds.Contains(m.PlayerId))
            .ToListAsync(cancellationToken);

        foreach (var member in members)
        {
            member.SelectionCount++;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetReadyAsync(
        int groupId,
        int playerId,
        bool isReady,
        CancellationToken cancellationToken = default)
    {
        var member = await _db.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.PlayerId == playerId, cancellationToken)
            ?? throw new InvalidOperationException("Вы не состоите в этой группе.");

        member.IsReady = isReady;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
