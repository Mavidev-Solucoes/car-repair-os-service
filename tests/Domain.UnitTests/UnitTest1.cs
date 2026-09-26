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
        var initialEventCount = serviceOrder.DomainEvents.Count;

        var act = () => serviceOrder.Close(assignedUserId);

        Assert.Throws<InvalidOperationException>(act);
        Assert.Equal(initialEventCount, serviceOrder.DomainEvents.Count);
    }

    [Fact]
    public void Cancel_AfterClose_ShouldThrowInvalidOperationException()
    {
        var serviceOrder = CreateServiceOrder(out var assignedUserId);
        serviceOrder.Close(assignedUserId);
        var initialEventCount = serviceOrder.DomainEvents.Count;

        var act = () => serviceOrder.Cancel(assignedUserId);

        Assert.Throws<InvalidOperationException>(act);
        Assert.Equal(initialEventCount, serviceOrder.DomainEvents.Count);
    }

    [Fact]
    public void Close_WhenAlreadyClosed_ShouldThrowInvalidOperationException()
    {
        var serviceOrder = CreateServiceOrder(out var assignedUserId);
        serviceOrder.Close(assignedUserId);
        var initialEventCount = serviceOrder.DomainEvents.Count;

        var act = () => serviceOrder.Close(assignedUserId);

        Assert.Throws<InvalidOperationException>(act);
        Assert.Equal(initialEventCount, serviceOrder.DomainEvents.Count);
    }

    [Fact]
    public void Cancel_WithDifferentRequestingUser_ShouldThrowInvalidOperationException()
    {
        var serviceOrder = CreateServiceOrder(out _);
        var initialEventCount = serviceOrder.DomainEvents.Count;

        var act = () => serviceOrder.Cancel(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(act);
        Assert.Equal(initialEventCount, serviceOrder.DomainEvents.Count);
    }

    [Fact]
    public void Close_WithDifferentRequestingUser_ShouldThrowInvalidOperationException()
    {
        var serviceOrder = CreateServiceOrder(out _);
        var initialEventCount = serviceOrder.DomainEvents.Count;

        var act = () => serviceOrder.Close(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(act);
        Assert.Equal(initialEventCount, serviceOrder.DomainEvents.Count);
    }

    [Fact]
    public void AddServiceItem_ShouldUpdateTotalPrice_AndTransitionToDiagnosing()
    {
        var serviceOrder = CreateServiceOrder(out var assignedUserId);
        serviceOrder.ClearDomainEvents();
        var item = new ServiceOrderItem(serviceOrder.Id, Guid.NewGuid(), " Brake pad replacement ", 120.50m, 2, assignedUserId);

        serviceOrder.AddServiceItem(item, assignedUserId);

        Assert.Equal(241.00m, serviceOrder.TotalPrice);
        Assert.Equal(Domain.Enums.ServiceStatus.Diagnosing, serviceOrder.Status);
        Assert.Collection(
            serviceOrder.StatusHistory,
            initial =>
            {
                Assert.Null(initial.FromStatus);
                Assert.Equal(Domain.Enums.ServiceStatus.Received, initial.ToStatus);
            },
            transition =>
            {
                Assert.Equal(Domain.Enums.ServiceStatus.Received, transition.FromStatus);
                Assert.Equal(Domain.Enums.ServiceStatus.Diagnosing, transition.ToStatus);
                Assert.Equal(assignedUserId, transition.ChangedByUserId);
            });
    }

    [Fact]
    public void AddServiceItem_WithDifferentUser_ShouldThrowInvalidOperationException()
    {
        var serviceOrder = CreateServiceOrder(out _);
        var item = new ServiceOrderItem(serviceOrder.Id, Guid.NewGuid(), "Inspection", 10m, 1);

        var act = () => serviceOrder.AddServiceItem(item, Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(act);
    }

    private static ServiceOrder CreateServiceOrder(out Guid assignedUserId)
    {
        assignedUserId = Guid.NewGuid();
        return new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), assignedUserId);
    }
}

public class CustomerEntityTests
{
    [Fact]
    public void Constructor_ShouldNormalizeCustomerFields_AndRaiseDomainEvent()
    {
        var customer = new Customer("  Maria   Silva  ", "123.456.789-09", " USER@Example.COM ", "(11) 99999-0000");

        Assert.Equal("Maria Silva", customer.Name);
        Assert.Equal("12345678909", customer.PersonalId);
        Assert.Equal("user@example.com", customer.Email);
        Assert.Equal("11999990000", customer.Telephone);
        Assert.True(customer.IsActive);
        Assert.Single(customer.DomainEvents.OfType<CustomerCreatedDomainEvent>());
    }
}

public class VehicleEntityTests
{
    [Fact]
    public void Update_ShouldNormalizeLicensePlate()
    {
        var vehicle = new Vehicle(Guid.NewGuid(), "Ford", "Ka", 2020, "abc-1234");

        vehicle.Update("Ford", "Ka", 2021, "def-5678", "Black");

        Assert.Equal("DEF5678", vehicle.LicensePlate);
        Assert.Equal(2021, vehicle.Year);
        Assert.Equal("Black", vehicle.Color);
    }
}
