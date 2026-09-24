using ItisDota.Data.Entities;

namespace ItisDota.Data.Repositories;

public interface IPlayerRepository
{
    Task<IReadOnlyList<Player>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Player?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task UpsertAsync(Player player, CancellationToken cancellationToken = default);
    Task UpdateAsync(Player player, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task IncrementSelectionCountsAsync(IReadOnlyCollection<int> playerIds, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
