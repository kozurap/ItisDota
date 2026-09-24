using System.Text;
using WebApplication1.Business.Extensions;
using WebApplication1.Business.Models;
using WebApplication1.Data.Entities;
using WebApplication1.Data.Repositories;

namespace WebApplication1.Business.Services;

public class PromptService
{
    public const string TeamPromptKey = "TeamPrompt";

    public const string DefaultPrompt =
        "Мы собираемся играть в доту 5 на 5. Вот список игроков, их ПТС рейтинг и их приоритеты по ролям (1-керри, 2 - мид, 3 - сложная, 4- софт саппорт, 5 -хард саппорт). Составь команды по 5 человек, где каждый играет на наиболее комфортной роли и поставь их против друг друга так, чтобы игры были равными. Учитывай, что играя на первом приоритете следует считать что игрок обладает 100% птс, второй приоритет - 95%, 3 - 90%, 4 - 80%, 5 - 70%. Дай объяснение почему ты так распределил команды а так же в конце укажи команды не в таблице, а в виде:\n" +
        "*Команда (силы тьмы или света)*\n" +
        "Имя, Тег в тг, птс\n" +
        "Имя, Тег в тг, птс\n" +
        "...";

    private readonly IAppSettingRepository _settings;
    private readonly IPlayerRepository _players;
    private readonly PlayerImportService _importService;

    public PromptService(
        IAppSettingRepository settings,
        IPlayerRepository players,
        PlayerImportService importService)
    {
        _settings = settings;
        _players = players;
        _importService = importService;
    }

    public async Task<string> GetPromptAsync(CancellationToken cancellationToken = default)
    {
        return await _settings.GetValueAsync(TeamPromptKey, cancellationToken) ?? DefaultPrompt;
    }

    public Task SavePromptAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return _settings.SetValueAsync(TeamPromptKey, prompt.Trim(), cancellationToken);
    }

    public async Task ImportPlayersAsync(string json, CancellationToken cancellationToken = default)
    {
        var players = _importService.Parse(json);

        foreach (var player in players)
        {
            await _players.UpsertAsync(player, cancellationToken);
        }

        await _players.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlayerDto>> GetPlayersAsync(CancellationToken cancellationToken = default)
    {
        var players = await _players.GetAllAsync(cancellationToken);
        return players.ToDtoList();
    }

    public Task DeletePlayerAsync(int id, CancellationToken cancellationToken = default)
    {
        return _players.DeleteAsync(id, cancellationToken);
    }

    public async Task UpdatePlayerAsync(PlayerEditDto edit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(edit);

        var realName = edit.RealName.Trim();
        var tgNick = edit.TgNick.Trim();
        var mmr = edit.MmRRange.Trim();

        if (string.IsNullOrWhiteSpace(realName))
        {
            throw new InvalidOperationException("Имя не может быть пустым.");
        }

        if (string.IsNullOrWhiteSpace(tgNick))
        {
            throw new InvalidOperationException("Telegram-ник не может быть пустым.");
        }

        if (tgNick.Contains('@', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Не указывайте символ @ в нике — он добавляется автоматически.");
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

        await _players.UpdateAsync(new Player
        {
            Id = edit.Id,
            RealName = realName,
            TgTag = "@" + tgNick,
            MmRRange = mmr,
            RolePriority1 = edit.RolePriority1,
            RolePriority2 = edit.RolePriority2,
            RolePriority3 = edit.RolePriority3,
            RolePriority4 = edit.RolePriority4,
            RolePriority5 = edit.RolePriority5
        }, cancellationToken);
    }

    public Task RecordSelectionsAsync(IReadOnlyCollection<int> playerIds, CancellationToken cancellationToken = default)
    {
        return _players.IncrementSelectionCountsAsync(playerIds, cancellationToken);
    }

    private static void ValidateRole(int role, int priorityIndex)
    {
        if (role is < 1 or > 5)
        {
            throw new InvalidOperationException($"Приоритет {priorityIndex}: роль должна быть числом от 1 до 5.");
        }
    }

    public static string FormatPlayerLine(PlayerDto player)
    {
        return $"{player.RealName} ({player.TgTag}) — MMR: {player.MmRRange}; приоритеты: " +
               $"1→{player.RolePriority1}, 2→{player.RolePriority2}, 3→{player.RolePriority3}, " +
               $"4→{player.RolePriority4}, 5→{player.RolePriority5}";
    }

    public static string BuildFullPrompt(string basePrompt, IEnumerable<PlayerDto> selectedPlayers)
    {
        var sb = new StringBuilder();
        sb.AppendLine(basePrompt.Trim());
        sb.AppendLine();
        sb.AppendLine("Игроки:");
        foreach (var player in selectedPlayers)
        {
            sb.AppendLine("- " + FormatPlayerLine(player));
        }

        return sb.ToString().TrimEnd();
    }
}
