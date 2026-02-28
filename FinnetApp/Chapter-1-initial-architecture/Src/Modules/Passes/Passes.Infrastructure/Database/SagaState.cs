namespace EvolutionaryArchitecture.Fitnet.Passes.Infrastructure.Database;

using Application;

internal sealed class SagaState
{
    public Guid SagaId { get; set; }
    public Guid CorrelationId { get; set; }
    public SagaStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
