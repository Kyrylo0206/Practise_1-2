namespace EvolutionaryArchitecture.Fitnet.Passes.Application;

using Domain;

internal class PassService(IPassRepository passRepository) : IPassService
{
    public async Task<Guid> RegisterPassAsync(Guid customerId, DateTimeOffset validFrom, DateTimeOffset validTo,
        CancellationToken cancellationToken = default)
    {
        var pass = Pass.Register(customerId, validFrom, validTo);
        await passRepository.AddAsync(pass, cancellationToken);
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
        await passRepository.SaveChangesAsync(cancellationToken);
    }
}
