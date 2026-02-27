namespace EvolutionaryArchitecture.Fitnet.Passes.Infrastructure.Database;

using Application;
using Microsoft.EntityFrameworkCore;

internal sealed class OutboxRepository(PassesPersistence persistence) : IOutboxRepository
{
    public async Task AddAsync(OutboxMessageDto message, CancellationToken cancellationToken = default)
    {
        var entity = new OutboxMessage
        {
            Id = message.Id,
            Type = message.Type,
            Payload = message.Payload,
            CreatedAt = message.CreatedAt,
            ProcessedAt = null
        };
        await persistence.OutboxMessages.AddAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyCollection<OutboxMessageDto>> GetUnprocessedAsync(CancellationToken cancellationToken = default)
    {
        var messages = await persistence.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        return [.. messages.Select(m => new OutboxMessageDto(m.Id, m.Type, m.Payload, m.CreatedAt, m.ProcessedAt))];
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await persistence.OutboxMessages.FindAsync([messageId], cancellationToken: cancellationToken);

        message!.ProcessedAt = DateTime.UtcNow;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await persistence.SaveChangesAsync(cancellationToken);
}
