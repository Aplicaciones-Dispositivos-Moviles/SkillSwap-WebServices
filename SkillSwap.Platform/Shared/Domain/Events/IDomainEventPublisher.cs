namespace SkillSwap.Platform.Shared.Domain.Events;

/// <summary>
///     Publishes domain events in process. It must be called after the change was saved. A handler
///     failure never reaches the publisher's caller: it is only logged.
/// </summary>
public interface IDomainEventPublisher
{
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken) where TEvent : IDomainEvent;
}