namespace EvolutionaryArchitecture.Fitnet.Passes.Infrastructure.Database;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class SagaStateConfiguration : IEntityTypeConfiguration<SagaState>
{
    public void Configure(EntityTypeBuilder<SagaState> builder)
    {
        builder.ToTable("SagaStates");
        builder.HasKey(s => s.SagaId);
        builder.Property(s => s.CorrelationId).IsRequired();
        builder.Property(s => s.Status).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.HasIndex(s => s.CorrelationId).IsUnique();
    }
}
