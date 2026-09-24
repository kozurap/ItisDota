using System.Text;
using ItisDota.Business.Models;

namespace ItisDota.Business.Services;

public class PromptService
{
    public const string DefaultPrompt =
        "Мы собираемся играть в доту 5 на 5. Вот список игроков, их ПТС рейтинг и их приоритеты по ролям (1-керри, 2 - мид, 3 - сложная, 4- софт саппорт, 5 -хард саппорт). Составь команды по 5 человек, где каждый играет на наиболее комфортной роли и поставь их против друг друга так, чтобы игры были равными. Учитывай, что играя на первом приоритете следует считать что игрок обладает 100% птс, второй приоритет - 95%, 3 - 90%, 4 - 80%, 5 - 70%. Дай объяснение почему ты так распределил команды а так же в конце укажи команды не в таблице, а в виде:\n" +
        "*Команда (силы тьмы или света)*\n" +
        "Имя, Тег в тг, птс\n" +
        "Имя, Тег в тг, птс\n" +
        "...";

    public static string FormatPlayerLine(PlayerDto player)
    {
        return $"{player.RealName} ({player.TgTag}) — MMR: {player.Mmr}; приоритеты: " +
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
