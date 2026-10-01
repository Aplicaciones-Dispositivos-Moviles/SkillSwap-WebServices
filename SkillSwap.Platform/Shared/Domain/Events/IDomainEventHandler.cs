namespace SkillSwap.Platform.Shared.Domain.Events;

/// <summary>
///     Reacts to a domain event published by another bounded context.
/// </summary>
/// <typeparam name="TEvent">The event handled</typeparam>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}