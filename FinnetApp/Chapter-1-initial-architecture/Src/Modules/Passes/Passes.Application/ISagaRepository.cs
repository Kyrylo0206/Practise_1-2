namespace EvolutionaryArchitecture.Fitnet.Passes.Application;

public interface ISagaRepository
{
    Task AddAsync(SagaStateDto saga, CancellationToken cancellationToken = default);
    Task<SagaStateDto?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid sagaId, SagaStatus status, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
