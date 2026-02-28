namespace EvolutionaryArchitecture.Fitnet.Passes.Application;

public record SagaStateDto(Guid SagaId, Guid CorrelationId, SagaStatus Status, DateTime CreatedAt, DateTime UpdatedAt);
