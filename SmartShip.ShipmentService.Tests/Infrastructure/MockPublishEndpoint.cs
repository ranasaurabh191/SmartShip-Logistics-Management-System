using MassTransit;

namespace SmartShip.ShipmentService.Tests.Infrastructure;

public class MockPublishEndpoint : IPublishEndpoint
{
    private readonly List<object> _published = new();

    public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
    { _published.Add(message!); return Task.CompletedTask; }

    public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class
    { _published.Add(message!); return Task.CompletedTask; }

    public Task Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class
    { _published.Add(message!); return Task.CompletedTask; }

    public Task Publish(object message, CancellationToken cancellationToken = default)
    { _published.Add(message); return Task.CompletedTask; }

    public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default)
    { _published.Add(message); return Task.CompletedTask; }

    public Task Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    { _published.Add(message); return Task.CompletedTask; }

    public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    { _published.Add(message); return Task.CompletedTask; }

    public Task Publish<T>(object message, CancellationToken cancellationToken = default) where T : class
    { _published.Add(message); return Task.CompletedTask; }

    public Task Publish<T>(object message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class
    { _published.Add(message); return Task.CompletedTask; }

    public Task Publish<T>(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class
    { _published.Add(message); return Task.CompletedTask; }

    public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw new NotImplementedException();

    public bool WasPublished<T>() => _published.OfType<T>().Any();
    public T? GetPublished<T>() => _published.OfType<T>().FirstOrDefault();
    public IEnumerable<T> GetAllPublished<T>() => _published.OfType<T>();
    public void Reset() => _published.Clear();
}