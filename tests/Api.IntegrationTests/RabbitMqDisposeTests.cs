using Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Api.IntegrationTests;

public sealed class RabbitMqDisposeTests
{
    public static TheoryData<Exception> ShutdownExceptions => new()
    {
        new NullReferenceException(),
        new AlreadyClosedException(new ShutdownEventArgs(ShutdownInitiator.Peer, 320, "connection forced")),
        new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 200, "closed")),
        new IOException("socket closed")
    };

    [Theory]
    [MemberData(nameof(ShutdownExceptions))]
    public void DisposeQuietly_ShouldIgnoreErrorsOfAlreadyClosedResources(Exception exception)
    {
        var resource = new Throwing(exception);

        RabbitMqCommandConsumer.DisposeQuietly(resource);

        Assert.True(resource.Disposed);
    }

    [Fact]
    public void DisposeQuietly_ShouldNotHideOtherErrors()
    {
        Assert.Throws<InvalidOperationException>(() =>
            RabbitMqCommandConsumer.DisposeQuietly(new Throwing(new InvalidOperationException())));
    }

    private sealed class Throwing(Exception exception) : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
            throw exception;
        }
    }
}
