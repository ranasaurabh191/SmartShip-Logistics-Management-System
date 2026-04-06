using MassTransit;

namespace SmartShip.PaymentService.Tests.Mocks;

public class MockPublishEndpoint : IPublishEndpoint
{
    private readonly List<object> _messages = new();

    public IReadOnlyList<object> PublishedMessages => _messages;

    public bool WasPublished<T>() => _messages.OfType<T>().Any();
    public T? GetPublished<T>() => _messages.OfType<T>().FirstOrDefault();
    public void Reset() => _messages.Clear();

    public Task Publish<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    { _messages.Add(message!); return Task.CompletedTask; }

    public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe,
        CancellationToken cancellationToken = default)
        where T : class
    { _messages.Add(message!); return Task.CompletedTask; }

    public Task Publish<T>(T message, IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken = default)
        where T : class
    { _messages.Add(message!); return Task.CompletedTask; }

    public Task Publish<T>(object values, CancellationToken cancellationToken = default)
        where T : class
    { _messages.Add(values); return Task.CompletedTask; }

    public Task Publish<T>(object values, IPipe<PublishContext<T>> publishPipe,
        CancellationToken cancellationToken = default)
        where T : class
    { _messages.Add(values); return Task.CompletedTask; }

    public Task Publish<T>(object values, IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken = default)
        where T : class
    { _messages.Add(values); return Task.CompletedTask; }

    public Task Publish(object message, CancellationToken cancellationToken = default)
    { _messages.Add(message); return Task.CompletedTask; }

    public Task Publish(object message, Type messageType,
        CancellationToken cancellationToken = default)
    { _messages.Add(message); return Task.CompletedTask; }

    public Task Publish(object message, IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken = default)
    { _messages.Add(message); return Task.CompletedTask; }

    public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken = default)
    { _messages.Add(message); return Task.CompletedTask; }

    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
        => throw new NotImplementedException();
}