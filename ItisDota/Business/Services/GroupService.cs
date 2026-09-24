using System.Security.Cryptography;
using ItisDota.Business.Extensions;
using ItisDota.Business.Models;
using ItisDota.Data.Entities;
using ItisDota.Data.Repositories;

namespace ItisDota.Business.Services;

public class GroupService
{
    private readonly IGroupRepository _groups;
    private readonly IPlayerRepository _players;

    public GroupService(IGroupRepository groups, IPlayerRepository players)
    {
        _groups = groups;
        _players = players;
    }

    public async Task<IReadOnlyList<GroupDto>> GetMyGroupsAsync(
        string keycloakUserId,
        CancellationToken cancellationToken = default)
    {
        var player = await _players.GetByKeycloakIdAsync(keycloakUserId, cancellationToken);
        if (player is null)
        {
            return Array.Empty<GroupDto>();
        }

        var groups = await _groups.GetGroupsForPlayerAsync(player.Id, cancellationToken);
        var result = new List<GroupDto>(groups.Count);
        foreach (var group in groups)
        {
            result.Add(new GroupDto
            {
                Id = group.Id,
                Name = group.Name,
                InviteToken = group.InviteToken,
                MemberCount = await _groups.GetMemberCountAsync(group.Id, cancellationToken),
                IsOwner = group.OwnerPlayerId == player.Id
            });
        }

        return result;
    }

    public async Task<int> CreateAsync(
        string keycloakUserId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var player = await RequireProfileAsync(keycloakUserId, cancellationToken);
        var groupName = name.Trim();
        if (string.IsNullOrWhiteSpace(groupName))
        {
            throw new InvalidOperationException("Название группы не может быть пустым.");
        }

        var group = await _groups.AddAsync(
            new PlayerGroup
            {
                Name = groupName,
                InviteToken = CreateInviteToken(),
                OwnerPlayerId = player.Id,
                CreatedAt = DateTimeOffset.UtcNow
            },
            cancellationToken);

        await _groups.AddMemberAsync(group.Id, player.Id, cancellationToken);
        return group.Id;
    }

    public async Task<int> JoinByTokenAsync(
        string keycloakUserId,
        string inviteToken,
        CancellationToken cancellationToken = default)
    {
        var player = await RequireProfileAsync(keycloakUserId, cancellationToken);
        var group = await _groups.GetByInviteTokenAsync(inviteToken, cancellationToken)
            ?? throw new InvalidOperationException("Ссылка-приглашение недействительна.");

        if (!await _groups.IsMemberAsync(group.Id, player.Id, cancellationToken))
        {
            await _groups.AddMemberAsync(group.Id, player.Id, cancellationToken);
        }

        return group.Id;
    }

    public async Task<GroupRoomDto?> GetRoomAsync(
        string keycloakUserId,
        int groupId,
        CancellationToken cancellationToken = default)
    {
        var player = await _players.GetByKeycloakIdAsync(keycloakUserId, cancellationToken);
        if (player is null || !await _groups.IsMemberAsync(groupId, player.Id, cancellationToken))
        {
            return null;
        }

        var group = await _groups.GetByIdAsync(groupId, cancellationToken);
        if (group is null)
        {
            return null;
        }

        var members = await _groups.GetMembersAsync(groupId, cancellationToken);
        return new GroupRoomDto
        {
            Id = group.Id,
            Name = group.Name,
            InviteToken = group.InviteToken,
            PromptText = string.IsNullOrWhiteSpace(group.PromptText)
                ? PromptService.DefaultPrompt
                : group.PromptText,
            CurrentPlayerId = player.Id,
            Members = members
                .Where(m => m.Player is not null)
                .Select(m => ToRoomMember(m))
                .ToList()
        };
    }

    public async Task SetReadyAsync(
        string keycloakUserId,
        int groupId,
        bool isReady,
        CancellationToken cancellationToken = default)
    {
        var player = await RequireProfileAsync(keycloakUserId, cancellationToken);
        if (!await _groups.IsMemberAsync(groupId, player.Id, cancellationToken))
        {
            throw new InvalidOperationException("Вы не состоите в этой группе.");
        }

        if (isReady && !ProfileService.IsComplete(player.ToDto()))
        {
            throw new InvalidOperationException("Сначала заполните профиль: укажите ПТС и приоритеты ролей.");
        }

        await _groups.SetReadyAsync(groupId, player.Id, isReady, cancellationToken);
    }

    public async Task SavePromptAsync(
        string keycloakUserId,
        int groupId,
        string promptText,
        CancellationToken cancellationToken = default)
    {
        await RequireMembershipAsync(keycloakUserId, groupId, cancellationToken);
        if (string.IsNullOrWhiteSpace(promptText))
        {
            throw new InvalidOperationException("Промпт не может быть пустым.");
        }

        await _groups.SavePromptAsync(groupId, promptText.Trim(), cancellationToken);
    }

    public async Task RecordSelectionsAsync(
        string keycloakUserId,
        int groupId,
        IReadOnlyCollection<int> playerIds,
        CancellationToken cancellationToken = default)
    {
        await RequireMembershipAsync(keycloakUserId, groupId, cancellationToken);
        await _groups.IncrementSelectionCountsAsync(groupId, playerIds, cancellationToken);
    }

    private async Task<Player> RequireProfileAsync(string keycloakUserId, CancellationToken cancellationToken)
    {
        return await _players.GetByKeycloakIdAsync(keycloakUserId, cancellationToken)
            ?? throw new InvalidOperationException("Сначала заполните профиль игрока.");
    }

    private async Task RequireMembershipAsync(string keycloakUserId, int groupId, CancellationToken cancellationToken)
    {
        var player = await RequireProfileAsync(keycloakUserId, cancellationToken);
        if (!await _groups.IsMemberAsync(groupId, player.Id, cancellationToken))
        {
            throw new InvalidOperationException("Вы не состоите в этой группе.");
        }
    }

    private static RoomMemberDto ToRoomMember(GroupMember member)
    {
        var player = member.Player!;
        return new RoomMemberDto
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
            RolePriority5 = player.RolePriority5,
            IsReady = member.IsReady
        };
    }

    private static string CreateInviteToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
