namespace EvolutionaryArchitecture.Fitnet.Passes.Application;

public record OutboxMessageDto(Guid Id, string Type, string Payload, DateTime CreatedAt, DateTime? ProcessedAt);
