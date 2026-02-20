namespace EvolutionaryArchitecture.Fitnet.Passes.Application;

using Domain;

public interface IPassRepository
{
    Task<Pass?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Pass>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Pass pass, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
