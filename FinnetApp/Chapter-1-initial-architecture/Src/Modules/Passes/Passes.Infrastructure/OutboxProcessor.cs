namespace EvolutionaryArchitecture.Fitnet.Passes.Infrastructure;

using System.Text.Json;
using Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal sealed partial class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    private static readonly TimeSpan Delay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessOutboxMessagesAsync(stoppingToken);
            await Task.Delay(Delay, stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var sagaRepository = scope.ServiceProvider.GetRequiredService<ISagaRepository>();

        var messages = await outboxRepository.GetUnprocessedAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                LogProcessingMessage(logger, message.Id, message.Type);

                await HandleMessageAsync(message, sagaRepository, cancellationToken);

                await outboxRepository.MarkAsProcessedAsync(message.Id, cancellationToken);
                await outboxRepository.SaveChangesAsync(cancellationToken);

                LogMessageProcessed(logger, message.Id);
            }
            catch (Exception ex)
            {
                LogProcessingFailed(logger, ex, message.Id);
            }
        }
    }

    private static async Task HandleMessageAsync(
        OutboxMessageDto message,
        ISagaRepository sagaRepository,
        CancellationToken cancellationToken)
    {
        var payload = JsonDocument.Parse(message.Payload);
        var correlationId = Guid.Parse(payload.RootElement.GetProperty("id").GetString()!);

        var saga = await sagaRepository.GetByCorrelationIdAsync(correlationId, cancellationToken);

        if (saga is null)
        {
            return;
        }

        if (saga.Status is SagaStatus.Completed or SagaStatus.Failed)
        {
            return;
        }

        await sagaRepository.UpdateStatusAsync(saga.SagaId, SagaStatus.Completed, cancellationToken);
        await sagaRepository.SaveChangesAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing outbox message {MessageId} of type {Type}")]
    private static partial void LogProcessingMessage(ILogger logger, Guid messageId, string type);

    [LoggerMessage(Level = LogLevel.Information, Message = "Outbox message {MessageId} successfully")]
    private static partial void LogMessageProcessed(ILogger logger, Guid messageId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to process message {MessageId}")]
    private static partial void LogProcessingFailed(ILogger logger, Exception ex, Guid messageId);
}
