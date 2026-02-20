namespace EvolutionaryArchitecture.Fitnet.Passes;

using Application;
using Common.Events.EventBus;
using MarkPassAsExpired.Events;

internal static class PassesEndpoints
{
    internal static void MapPassEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet($"{ApiPaths.Root}/passes",
                async (IPassService passService, CancellationToken cancellationToken) =>
                {
                    var passes = await passService.GetAllPassesAsync(cancellationToken);
                    return Results.Ok(new { Passes = passes });
                })
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);

        app.MapPatch($"{ApiPaths.Root}/passes/{{id}}",
                async (
                    Guid id,
                    IPassService passService,
                    TimeProvider timeProvider,
                    IEventBus eventBus,
                    CancellationToken cancellationToken) =>
                {
                    var nowDate = timeProvider.GetUtcNow();
                    try
                    {
                        await passService.MarkPassAsExpiredAsync(id, nowDate, cancellationToken);
                    }
                    catch (InvalidOperationException)
                    {
                        return Results.NotFound();
                    }

                    await eventBus.PublishAsync(
                        PassExpiredEvent.Create(id, Guid.Empty, nowDate),
                        cancellationToken);

                    return Results.NoContent();
                })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}