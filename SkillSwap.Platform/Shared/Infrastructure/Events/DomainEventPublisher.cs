using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkillSwap.Platform.Shared.Domain.Events;

namespace SkillSwap.Platform.Shared.Infrastructure.Events;

/// <summary>
///     In-process domain event publisher. It resolves the handlers registered for the event and runs
///     them one after another; a failing handler is logged and never stops the others or the caller.
/// </summary>
/// <param name="serviceProvider">The scoped service provider of the request</param>
/// <param name="logger">Logger</param>
public class DomainEventPublisher(IServiceProvider serviceProvider, ILogger<DomainEventPublisher> logger)
    : IDomainEventPublisher
{
    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        foreach (var handler in serviceProvider.GetServices<IDomainEventHandler<TEvent>>())
            try
            {
                await handler.HandleAsync(domainEvent, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Handler {Handler} failed for the event {Event}",
                    handler.GetType().Name, typeof(TEvent).Name);
            }
    }
}