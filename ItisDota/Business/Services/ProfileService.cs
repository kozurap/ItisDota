using ItisDota.Business.Extensions;
using ItisDota.Business.Models;
using ItisDota.Data.Entities;
using ItisDota.Data.Repositories;

namespace ItisDota.Business.Services;

public class ProfileService
{
    private readonly IPlayerRepository _players;

    public ProfileService(IPlayerRepository players)
    {
        _players = players;
    }

    public static bool IsComplete(PlayerDto profile)
    {
        return !string.IsNullOrWhiteSpace(profile.MmRRange)
               && IsRole(profile.RolePriority1)
               && IsRole(profile.RolePriority2)
               && IsRole(profile.RolePriority3)
               && IsRole(profile.RolePriority4)
               && IsRole(profile.RolePriority5);
    }

    public async Task<PlayerDto?> GetAsync(string keycloakUserId, CancellationToken cancellationToken = default)
    {
        var player = await _players.GetByKeycloakIdAsync(keycloakUserId, cancellationToken);
        return player?.ToDto();
    }

    public async Task CreateAsync(
        string keycloakUserId,
        string realName,
        string tgNick,
        CancellationToken cancellationToken = default)
    {
        var tgTag = NormalizeTgTag(tgNick);
        if (await _players.IsTgTagTakenAsync(tgTag, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException($"Telegram {tgTag} уже занят другим игроком.");
        }

        await _players.AddAsync(
            new Player
            {
                KeycloakUserId = keycloakUserId,
                RealName = realName.Trim(),
                TgTag = tgTag,
                MmRRange = string.Empty
            },
            cancellationToken);
    }

    public async Task EnsureTgTagFreeAsync(string tgNick, CancellationToken cancellationToken = default)
    {
        var tgTag = NormalizeTgTag(tgNick);
        if (await _players.IsTgTagTakenAsync(tgTag, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException($"Telegram {tgTag} уже занят другим игроком.");
        }
    }

    public async Task SaveAsync(
        string keycloakUserId,
        PlayerEditDto edit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(edit);

        var player = await _players.GetByKeycloakIdAsync(keycloakUserId, cancellationToken)
            ?? throw new InvalidOperationException("Профиль не найден.");

        var realName = edit.RealName.Trim();
        var mmr = edit.MmRRange.Trim();
        var tgTag = NormalizeTgTag(edit.TgNick);

        if (string.IsNullOrWhiteSpace(realName))
        {
            throw new InvalidOperationException("Имя не может быть пустым.");
        }

        if (string.IsNullOrWhiteSpace(mmr))
        {
            throw new InvalidOperationException("ПТС не может быть пустым.");
        }

        ValidateRole(edit.RolePriority1, 1);
        ValidateRole(edit.RolePriority2, 2);
        ValidateRole(edit.RolePriority3, 3);
        ValidateRole(edit.RolePriority4, 4);
        ValidateRole(edit.RolePriority5, 5);

        if (await _players.IsTgTagTakenAsync(tgTag, player.Id, cancellationToken))
        {
            throw new InvalidOperationException($"Telegram {tgTag} уже занят другим игроком.");
        }

        player.RealName = realName;
        player.TgTag = tgTag;
        player.MmRRange = mmr;
        player.RolePriority1 = edit.RolePriority1;
        player.RolePriority2 = edit.RolePriority2;
        player.RolePriority3 = edit.RolePriority3;
        player.RolePriority4 = edit.RolePriority4;
        player.RolePriority5 = edit.RolePriority5;

        await _players.UpdateAsync(player, cancellationToken);
    }

    public static string NormalizeTgTag(string tgNick)
    {
        var nick = tgNick.Trim();
        if (string.IsNullOrWhiteSpace(nick))
        {
            throw new InvalidOperationException("Telegram-ник не может быть пустым.");
        }

        if (nick.Contains('@', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Не указывайте символ @ в нике — он добавляется автоматически.");
        }

        return "@" + nick;
    }

    private static bool IsRole(int role) => role is >= 1 and <= 5;

    private static void ValidateRole(int role, int priorityIndex)
    {
        if (!IsRole(role))
        {
            throw new InvalidOperationException($"Приоритет {priorityIndex}: роль должна быть числом от 1 до 5.");
        }
    }
}
