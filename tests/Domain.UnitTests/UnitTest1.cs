using Domain.Entities;
using Domain.Events;

namespace Domain.UnitTests;

public class ServiceOrderDomainEventsTests
{
    [Fact]
    public void Constructor_ShouldRaiseServiceOrderOpenedDomainEvent()
    {
        var serviceOrder = CreateServiceOrder(out _);

        var openedEvent = Assert.Single(serviceOrder.DomainEvents.OfType<ServiceOrderOpenedDomainEvent>());
        Assert.Equal(serviceOrder.Id, openedEvent.ServiceOrderId);
    }

    [Fact]
    public void Cancel_ShouldRaiseServiceOrderCancelledDomainEvent()
    {
        var serviceOrder = CreateServiceOrder(out var assignedUserId);
        serviceOrder.ClearDomainEvents();

        serviceOrder.Cancel(assignedUserId);

        var cancelledEvent = Assert.Single(serviceOrder.DomainEvents.OfType<ServiceOrderCancelledDomainEvent>());
        Assert.Equal(serviceOrder.Id, cancelledEvent.ServiceOrderId);
        Assert.Equal(assignedUserId, cancelledEvent.CancelledByUserId);
    }

    [Fact]
    public void Close_ShouldRaiseServiceOrderClosedDomainEvent()
    {
        var serviceOrder = CreateServiceOrder(out var assignedUserId);
        serviceOrder.ClearDomainEvents();

        serviceOrder.Close(assignedUserId);

        var closedEvent = Assert.Single(serviceOrder.DomainEvents.OfType<ServiceOrderClosedDomainEvent>());
        Assert.Equal(serviceOrder.Id, closedEvent.ServiceOrderId);
        Assert.Equal(assignedUserId, closedEvent.ClosedByUserId);
    }

    [Fact]
    public void Close_AfterCancel_ShouldThrowInvalidOperationException()
    {
        var serviceOrder = CreateServiceOrder(out var assignedUserId);
        serviceOrder.Cancel(assignedUserId);

        var act = () => serviceOrder.Close(assignedUserId);

        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Cancel_AfterClose_ShouldThrowInvalidOperationException()
    {
        var serviceOrder = CreateServiceOrder(out var assignedUserId);
        serviceOrder.Close(assignedUserId);

        var act = () => serviceOrder.Cancel(assignedUserId);

        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Close_WhenAlreadyClosed_ShouldThrowInvalidOperationException()
    {
        var serviceOrder = CreateServiceOrder(out var assignedUserId);
        serviceOrder.Close(assignedUserId);

        var act = () => serviceOrder.Close(assignedUserId);

        Assert.Throws<InvalidOperationException>(act);
    }

    private static ServiceOrder CreateServiceOrder(out Guid assignedUserId)
    {
        assignedUserId = Guid.NewGuid();
        return new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), assignedUserId);
    }
}
