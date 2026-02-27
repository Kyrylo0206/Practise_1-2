namespace EvolutionaryArchitecture.Fitnet.Passes.Application;

using System.Text.Json;
using Domain;

internal class PassService(
    IPassRepository passRepository,
    IOutboxRepository outboxRepository,
    ISagaRepository sagaRepository) : IPassService
{
    public async Task<Guid> RegisterPassAsync(Guid customerId, DateTimeOffset validFrom, DateTimeOffset validTo,
        CancellationToken cancellationToken = default)
    {
        var pass = Pass.Register(customerId, validFrom, validTo);
        await passRepository.AddAsync(pass, cancellationToken);

        var outboxMessage = new OutboxMessageDto(
            Guid.NewGuid(),
            "PassRegistered",
            JsonSerializer.Serialize(new { @event = "PassRegistered", id = pass.Id }),
            DateTime.UtcNow,
            null);
        await outboxRepository.AddAsync(outboxMessage, cancellationToken);

        var saga = new SagaStateDto(
            Guid.NewGuid(),
            pass.Id,
            SagaStatus.Started,
            DateTime.UtcNow,
            DateTime.UtcNow);
        await sagaRepository.AddAsync(saga, cancellationToken);

        await passRepository.SaveChangesAsync(cancellationToken);

        return pass.Id;
    }

    public async Task<IReadOnlyCollection<PassDto>> GetAllPassesAsync(
        CancellationToken cancellationToken = default)
    {
        var passes = await passRepository.GetAllAsync(cancellationToken);
        return [.. passes.Select(p => new PassDto(p.Id, p.CustomerId, p.From, p.To))];
    }

    public async Task MarkPassAsExpiredAsync(Guid passId, DateTimeOffset expiredAt,
        CancellationToken cancellationToken = default)
    {
        var pass = await passRepository.GetByIdAsync(passId, cancellationToken)
            ?? throw new InvalidOperationException($"Pass with id {passId} was not found.");

        pass.MarkAsExpired(expiredAt);

        var outboxMessage = new OutboxMessageDto(
            Guid.NewGuid(),
            "PassExpired",
            JsonSerializer.Serialize(new { @event = "PassExpired", id = pass.Id }),
            DateTime.UtcNow,
            null);
        await outboxRepository.AddAsync(outboxMessage, cancellationToken);

        await passRepository.SaveChangesAsync(cancellationToken);
    }
}
