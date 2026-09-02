using DeskTodo.Application.Events;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeskTodo.Tests.Application;

public class InMemoryEventBusTests
{
    private readonly InMemoryEventBus _sut = new(NullLogger<InMemoryEventBus>.Instance);

    private static ApplicationEvent MakeEvent(string eventType) =>
        new(eventType, Guid.NewGuid(), DateTime.UtcNow, "Test", PayloadJson: null);

    [Fact]
    public void Publish_WithAnUnfilteredSubscriber_DeliversEveryEvent()
    {
        var received = new List<ApplicationEvent>();
        using var subscription = _sut.Subscribe(received.Add);

        _sut.Publish(MakeEvent("TaskCreated"));
        _sut.Publish(MakeEvent("TaskCompleted"));

        Assert.Equal(2, received.Count);
    }

    [Fact]
    public void Publish_WithAFilteredSubscriber_OnlyDeliversMatchingEvents()
    {
        var received = new List<ApplicationEvent>();
        using var subscription = _sut.Subscribe("TaskCompleted", received.Add);

        _sut.Publish(MakeEvent("TaskCreated"));
        _sut.Publish(MakeEvent("TaskCompleted"));

        var single = Assert.Single(received);
        Assert.Equal("TaskCompleted", single.EventType);
    }

    [Fact]
    public void Publish_AfterUnsubscribing_NoLongerDeliversToThatHandler()
    {
        var received = new List<ApplicationEvent>();
        var subscription = _sut.Subscribe(received.Add);
        subscription.Dispose();

        _sut.Publish(MakeEvent("TaskCreated"));

        Assert.Empty(received);
    }

    [Fact]
    public void Publish_WhenOneSubscriberThrows_StillDeliversToTheOthers()
    {
        var received = new List<ApplicationEvent>();
        using var throwing = _sut.Subscribe(_ => throw new InvalidOperationException("boom"));
        using var healthy = _sut.Subscribe(received.Add);

        _sut.Publish(MakeEvent("TaskCreated"));

        Assert.Single(received);
    }

    [Fact]
    public void Publish_WithMultipleSubscribers_DeliversToAllOfThem()
    {
        var countA = 0;
        var countB = 0;
        using var a = _sut.Subscribe(_ => countA++);
        using var b = _sut.Subscribe(_ => countB++);

        _sut.Publish(MakeEvent("TaskCreated"));

        Assert.Equal(1, countA);
        Assert.Equal(1, countB);
    }
}
