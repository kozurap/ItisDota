using System.Text.Json;
using ItisDota.Data.Entities;

namespace ItisDota.Business.Services;

public class PlayerImportService
{
    private static readonly Dictionary<string, Action<Player, string>> FieldSetters =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ирл имя"] = (p, v) => p.RealName = v.Trim(),
            ["тег в tg"] = (p, v) => p.TgTag = v.Trim(),
            ["сколько птс"] = (p, v) => p.MmRRange = v.Trim(),
            ["роль (1-й приоритет)"] = (p, v) => p.RolePriority1 = ParseRole(v),
            ["роль (2-й приоритет)"] = (p, v) => p.RolePriority2 = ParseRole(v),
            ["роль (3-й приоритет)"] = (p, v) => p.RolePriority3 = ParseRole(v),
            ["роль (4-й приоритет)"] = (p, v) => p.RolePriority4 = ParseRole(v),
            ["роль (5-й приоритет)"] = (p, v) => p.RolePriority5 = ParseRole(v),
        };

    public IReadOnlyList<Player> Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("JSON должен быть массивом игроков.");
        }

        var players = new List<Player>();
        foreach (var playerElement in document.RootElement.EnumerateArray())
        {
            if (playerElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Каждый игрок должен быть массивом пар [поле, значение].");
            }

            var player = new Player();
            foreach (var pair in playerElement.EnumerateArray())
            {
                if (pair.ValueKind != JsonValueKind.Array || pair.GetArrayLength() < 2)
                {
                    continue;
                }

                var key = NormalizeKey(pair[0].GetString() ?? string.Empty);
                var value = pair[1].GetString() ?? string.Empty;
                if (FieldSetters.TryGetValue(key, out var setter))
                {
                    setter(player, value);
                }
            }

            if (string.IsNullOrWhiteSpace(player.TgTag))
            {
                throw new InvalidOperationException("У одного из игроков отсутствует «Тег в tg».");
            }

            players.Add(player);
        }

        return players;
    }

    private static string NormalizeKey(string key) => key.Trim().ToLowerInvariant();

    private static int ParseRole(string value)
    {
        if (!int.TryParse(value.Trim(), out var role) || role is < 1 or > 5)
        {
            throw new InvalidOperationException($"Некорректная роль: «{value}». Ожидается число от 1 до 5.");
        }

        return role;
    }
}
