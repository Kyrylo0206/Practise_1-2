namespace EvolutionaryArchitecture.Fitnet.Passes.Infrastructure.Database;

using Application;
using Domain;
using Microsoft.EntityFrameworkCore;


internal sealed class PassRepository(PassesPersistence persistence) : IPassRepository
{
    public async Task<Pass?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await persistence.Passes.FindAsync([id], cancellationToken: cancellationToken);

    public async Task<IReadOnlyCollection<Pass>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await persistence.Passes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Pass pass, CancellationToken cancellationToken = default) =>
        await persistence.Passes.AddAsync(pass, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await persistence.SaveChangesAsync(cancellationToken);
}
