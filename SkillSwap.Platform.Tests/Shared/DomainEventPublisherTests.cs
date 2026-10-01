using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SkillSwap.Platform.Shared.Domain.Events;
using SkillSwap.Platform.Shared.Infrastructure.Events;

namespace SkillSwap.Platform.Tests.Shared;

public class DomainEventPublisherTests
{
    private sealed record SampleEvent(string Name) : IDomainEvent;

    private sealed record OtherEvent : IDomainEvent;

    private sealed class RecordingHandler : IDomainEventHandler<SampleEvent>
    {
        public List<string> Handled { get; } = [];

        public Task HandleAsync(SampleEvent domainEvent, CancellationToken cancellationToken)
        {
            Handled.Add(domainEvent.Name);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingHandler : IDomainEventHandler<SampleEvent>
    {
        public Task HandleAsync(SampleEvent domainEvent, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("boom");
        }
    }

    private sealed class OtherHandler : IDomainEventHandler<OtherEvent>
    {
        public int Calls { get; private set; }

        public Task HandleAsync(OtherEvent domainEvent, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private static DomainEventPublisher Publisher(Action<IServiceCollection> register)
    {
        var services = new ServiceCollection();
        register(services);
        return new DomainEventPublisher(services.BuildServiceProvider(), NullLogger<DomainEventPublisher>.Instance);
    }

    [Fact]
    public async Task Publish_RunsEveryHandlerOfTheEvent()
    {
        var first = new RecordingHandler();
        var second = new RecordingHandler();
        var publisher = Publisher(services =>
        {
            services.AddSingleton<IDomainEventHandler<SampleEvent>>(first);
            services.AddSingleton<IDomainEventHandler<SampleEvent>>(second);
        });

        await publisher.PublishAsync(new SampleEvent("passed"), CancellationToken.None);

        Assert.Equal(["passed"], first.Handled);
        Assert.Equal(["passed"], second.Handled);
    }

    [Fact]
    public async Task Publish_WhenAHandlerFails_DoesNotThrowAndTheOthersStillRun()
    {
        var recording = new RecordingHandler();
        var publisher = Publisher(services =>
        {
            services.AddSingleton<IDomainEventHandler<SampleEvent>>(new FailingHandler());
            services.AddSingleton<IDomainEventHandler<SampleEvent>>(recording);
        });

        await publisher.PublishAsync(new SampleEvent("passed"), CancellationToken.None);

        Assert.Equal(["passed"], recording.Handled);
    }

    [Fact]
    public async Task Publish_WithoutHandlers_Completes()
    {
        var publisher = Publisher(_ => { });

        await publisher.PublishAsync(new SampleEvent("passed"), CancellationToken.None);
    }

    [Fact]
    public async Task Publish_DoesNotRunTheHandlersOfOtherEvents()
    {
        var other = new OtherHandler();
        var publisher = Publisher(services => services.AddSingleton<IDomainEventHandler<OtherEvent>>(other));

        await publisher.PublishAsync(new SampleEvent("passed"), CancellationToken.None);

        Assert.Equal(0, other.Calls);
    }
}