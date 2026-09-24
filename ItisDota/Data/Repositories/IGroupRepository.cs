using ItisDota.Data.Entities;

namespace ItisDota.Data.Repositories;

public interface IGroupRepository
{
    Task<IReadOnlyList<PlayerGroup>> GetGroupsForPlayerAsync(int playerId, CancellationToken cancellationToken = default);
    Task<PlayerGroup?> GetByIdAsync(int groupId, CancellationToken cancellationToken = default);
    Task<PlayerGroup?> GetByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken = default);
    Task<PlayerGroup> AddAsync(PlayerGroup group, CancellationToken cancellationToken = default);
    Task<bool> IsMemberAsync(int groupId, int playerId, CancellationToken cancellationToken = default);
    Task AddMemberAsync(int groupId, int playerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GroupMember>> GetMembersAsync(int groupId, CancellationToken cancellationToken = default);
    Task<int> GetMemberCountAsync(int groupId, CancellationToken cancellationToken = default);
    Task SavePromptAsync(int groupId, string? promptText, CancellationToken cancellationToken = default);
    Task IncrementSelectionCountsAsync(
        int groupId,
        IReadOnlyCollection<int> playerIds,
        CancellationToken cancellationToken = default);
    Task SetReadyAsync(int groupId, int playerId, bool isReady, CancellationToken cancellationToken = default);
}
