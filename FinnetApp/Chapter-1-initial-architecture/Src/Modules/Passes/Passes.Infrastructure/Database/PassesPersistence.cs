namespace EvolutionaryArchitecture.Fitnet.Passes.Infrastructure.Database;

using Domain;
using Microsoft.EntityFrameworkCore;

internal sealed class PassesPersistence(DbContextOptions<PassesPersistence> options) : DbContext(options)
{
    private const string Schema = "Passes";

    public DbSet<Pass> Passes => Set<Pass>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<SagaState> SagaStates => Set<SagaState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new PassEntityConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new SagaStateConfiguration());
    }
}
