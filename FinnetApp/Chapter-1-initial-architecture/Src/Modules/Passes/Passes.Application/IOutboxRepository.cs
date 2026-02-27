namespace EvolutionaryArchitecture.Fitnet.Passes.Application;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessageDto message, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<OutboxMessageDto>> GetUnprocessedAsync(CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
