namespace EvolutionaryArchitecture.Fitnet.Passes.Application;

public interface IPassService
{
    Task<Guid> RegisterPassAsync(Guid customerId, DateTimeOffset validFrom, DateTimeOffset validTo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PassDto>> GetAllPassesAsync(CancellationToken cancellationToken = default);

    Task MarkPassAsExpiredAsync(Guid passId, DateTimeOffset expiredAt,
        CancellationToken cancellationToken = default);
}
