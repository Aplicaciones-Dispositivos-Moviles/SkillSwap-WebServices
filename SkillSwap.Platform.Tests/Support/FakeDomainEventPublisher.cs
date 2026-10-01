using SkillSwap.Platform.Shared.Domain.Events;

namespace SkillSwap.Platform.Tests.Support;

public class FakeDomainEventPublisher : IDomainEventPublisher
{
    public List<IDomainEvent> Published { get; } = [];

    public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        Published.Add(domainEvent);
        return Task.CompletedTask;
    }
}