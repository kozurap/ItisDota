using ItisDota.Data.Entities;

namespace ItisDota.Data.Repositories;

public interface IPlayerRepository
{
    Task<Player?> GetByKeycloakIdAsync(string keycloakUserId, CancellationToken cancellationToken = default);
    Task<bool> IsTgTagTakenAsync(string tgTag, int? exceptPlayerId = null, CancellationToken cancellationToken = default);
    Task<Player> AddAsync(Player player, CancellationToken cancellationToken = default);
    Task UpdateAsync(Player player, CancellationToken cancellationToken = default);
}
