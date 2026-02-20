namespace EvolutionaryArchitecture.Fitnet.Passes.RegisterPass;

using Application;
using Contracts.SignContract.Events;
using Events;
using EvolutionaryArchitecture.Fitnet.Common.Events;
using EvolutionaryArchitecture.Fitnet.Common.Events.EventBus;

internal sealed class ContractSignedEventHandler(
    IPassService passService,
    IEventBus eventBus) : IIntegrationEventHandler<ContractSignedEvent>
{
    public async Task Handle(ContractSignedEvent @event, CancellationToken cancellationToken)
    {
        var passId = await passService.RegisterPassAsync(
            @event.ContractCustomerId, @event.SignedAt, @event.ExpireAt, cancellationToken);

        var passRegisteredEvent = PassRegisteredEvent.Create(passId);
        await eventBus.PublishAsync(passRegisteredEvent, cancellationToken);
    }
}
