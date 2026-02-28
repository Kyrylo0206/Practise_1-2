namespace EvolutionaryArchitecture.Fitnet.Passes.Infrastructure.Database;

using Application;
using Microsoft.EntityFrameworkCore;

internal sealed class SagaRepository(PassesPersistence persistence) : ISagaRepository
{
    public async Task AddAsync(SagaStateDto saga, CancellationToken cancellationToken = default)
    {
        var entity = new SagaState
        {
            SagaId = saga.SagaId,
            CorrelationId = saga.CorrelationId,
            Status = saga.Status,
            CreatedAt = saga.CreatedAt,
            UpdatedAt = saga.UpdatedAt
        };
        await persistence.SagaStates.AddAsync(entity, cancellationToken);
    }

    public async Task<SagaStateDto?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        var entity = await persistence.SagaStates
            .FirstOrDefaultAsync(s => s.CorrelationId == correlationId, cancellationToken);

        return entity is null
            ? null
            : new SagaStateDto(entity.SagaId, entity.CorrelationId, entity.Status, entity.CreatedAt, entity.UpdatedAt);
    }

    public async Task UpdateStatusAsync(Guid sagaId, SagaStatus status, CancellationToken cancellationToken = default)
    {
        var entity = await persistence.SagaStates.FindAsync([sagaId], cancellationToken: cancellationToken);
        if (entity is not null)
        {
            entity.Status = status;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await persistence.SaveChangesAsync(cancellationToken);
}
