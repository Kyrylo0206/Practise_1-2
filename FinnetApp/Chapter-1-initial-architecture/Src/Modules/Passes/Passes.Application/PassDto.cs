namespace EvolutionaryArchitecture.Fitnet.Passes.Application;

public record PassDto(Guid Id, Guid CustomerId, DateTimeOffset From, DateTimeOffset To);
