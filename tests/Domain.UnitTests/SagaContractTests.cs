using System.Linq.Expressions;
using Application.Common.Exceptions;
using Application.Common.Messaging;
using Application.ServiceOrders.Saga;
using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace Domain.UnitTests;

public class SagaContractTests
{
    [Fact]
    public void CloseBySystem_ShouldCloseOnceAndRaiseEvent()
    {
        var serviceOrder = CreateServiceOrder();
        serviceOrder.ClearDomainEvents();

        Assert.True(serviceOrder.CloseBySystem());
        Assert.False(serviceOrder.CloseBySystem());

        Assert.Equal(ServiceStatus.Closed, serviceOrder.Status);
        var closed = Assert.Single(serviceOrder.DomainEvents.OfType<ServiceOrderClosedDomainEvent>());
        Assert.Equal(Guid.Empty, closed.ClosedByUserId);
        Assert.Null(serviceOrder.StatusHistory.Last().ChangedByUserId);
    }

    [Fact]
    public void CancelBySystem_ShouldCancelOnceAndRaiseEvent()
    {
        var serviceOrder = CreateServiceOrder();
        serviceOrder.ClearDomainEvents();

        Assert.True(serviceOrder.CancelBySystem());
        Assert.False(serviceOrder.CancelBySystem());

        Assert.Equal(ServiceStatus.Cancelled, serviceOrder.Status);
        Assert.Single(serviceOrder.DomainEvents.OfType<ServiceOrderCancelledDomainEvent>());
    }

    [Fact]
    public void SystemActions_ShouldNotCrossTerminalStates()
    {
        var closed = CreateServiceOrder();
        closed.CloseBySystem();
        Assert.Throws<InvalidOperationException>(() => closed.CancelBySystem());

        var cancelled = CreateServiceOrder();
        cancelled.CancelBySystem();
        Assert.Throws<InvalidOperationException>(() => cancelled.CloseBySystem());
    }

    [Fact]
    public void BuildOpenedPayload_ShouldFollowContract()
    {
        var userId = Guid.NewGuid();
        var serviceOrder = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), userId);
        serviceOrder.AddServiceItem(new ServiceOrderItem(serviceOrder.Id, Guid.NewGuid(), "Troca de óleo", 150m, 1), userId);
        serviceOrder.AddServiceItem(new ServiceOrderItem(serviceOrder.Id, Guid.NewGuid(), "Filtro", 40m, 2), userId);
        var customer = new Customer("Maria Silva", "123.456.789-09", "maria@example.com", "(11) 99999-0000");

        var payload = ServiceOrderIntegrationEventsHandler.BuildOpenedPayload(serviceOrder, customer);

        Assert.Equal(serviceOrder.Id, payload["serviceOrderId"]!.GetValue<Guid>());
        Assert.Equal("Maria Silva", payload["customerName"]!.GetValue<string>());
        Assert.Equal(230m, payload["totalAmount"]!.GetValue<decimal>());
        var items = payload["items"]!.AsArray();
        Assert.Equal(2, items.Count);
        Assert.Equal("Filtro", items[1]!["description"]!.GetValue<string>());
        Assert.Equal(2, items[1]!["quantity"]!.GetValue<int>());
    }

    [Fact]
    public async Task IntegrationEventsHandler_ShouldPublishOpenedWithServiceOrderCorrelation()
    {
        var serviceOrder = CreateServiceOrder();
        var publisher = new RecordingEventPublisher();
        var handler = new ServiceOrderIntegrationEventsHandler(
            new InMemoryServiceOrderRepository(serviceOrder),
            new NullCustomerRepository(),
            publisher,
            NullLogger<ServiceOrderIntegrationEventsHandler>.Instance);

        await handler.Handle(new ServiceOrderOpenedDomainEvent(serviceOrder.Id), CancellationToken.None);
        await handler.Handle(new ServiceOrderClosedDomainEvent(serviceOrder.Id, Guid.Empty), CancellationToken.None);
        await handler.Handle(new ServiceOrderCancelledDomainEvent(serviceOrder.Id, Guid.Empty), CancellationToken.None);
        await handler.Handle(new ServiceOrderOpenedDomainEvent(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(
            ["ServiceOrderOpened", "ServiceOrderClosed", "ServiceOrderCancelled"],
            publisher.Messages.Select(message => message.Type).ToArray());
        Assert.All(publisher.Messages, message => Assert.Equal(serviceOrder.Id.ToString("D"), message.CorrelationId));
        Assert.Null(publisher.Messages[0].Payload!["customerName"]);
    }

    [Fact]
    public async Task IntegrationEventsHandler_ShouldSwallowPublishFailures()
    {
        var serviceOrder = CreateServiceOrder();
        var handler = new ServiceOrderIntegrationEventsHandler(
            new InMemoryServiceOrderRepository(serviceOrder),
            new NullCustomerRepository(),
            new FailingEventPublisher(),
            NullLogger<ServiceOrderIntegrationEventsHandler>.Instance);

        await handler.Handle(new ServiceOrderClosedDomainEvent(serviceOrder.Id, Guid.Empty), CancellationToken.None);
    }

    [Fact]
    public async Task SagaHandlers_ShouldCloseAndCancelIdempotently()
    {
        var toClose = CreateServiceOrder();
        var toCancel = CreateServiceOrder();
        var unitOfWork = new CountingUnitOfWork();
        var handler = new SagaServiceOrderCommandHandlers(
            new InMemoryServiceOrderRepository(toClose, toCancel),
            unitOfWork,
            NullLogger<SagaServiceOrderCommandHandlers>.Instance);

        await handler.Handle(new CloseServiceOrderBySagaCommand(toClose.Id), CancellationToken.None);
        await handler.Handle(new CloseServiceOrderBySagaCommand(toClose.Id), CancellationToken.None);
        await handler.Handle(new CancelServiceOrderBySagaCommand(toCancel.Id), CancellationToken.None);

        Assert.Equal(ServiceStatus.Closed, toClose.Status);
        Assert.Equal(ServiceStatus.Cancelled, toCancel.Status);
        Assert.Equal(2, unitOfWork.Commits);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CancelServiceOrderBySagaCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void GetServiceOrderId_ShouldRejectInvalidCorrelation(string correlationId)
    {
        Assert.Throws<InvalidMessageException>(() => new IntegrationMessage { CorrelationId = correlationId }.GetServiceOrderId());
    }

    private static ServiceOrder CreateServiceOrder() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    private sealed class RecordingEventPublisher : IEventPublisher
    {
        public List<IntegrationMessage> Messages { get; } = [];

        public Task PublishAsync(IntegrationMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingEventPublisher : IEventPublisher
    {
        public Task PublishAsync(IntegrationMessage message, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("RabbitMQ fora do ar");
    }

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        public int Commits { get; private set; }

        public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            Commits++;
            return Task.FromResult(1);
        }
    }

    private sealed class InMemoryServiceOrderRepository(params ServiceOrder[] items) : IServiceOrderRepository
    {
        public Task<ServiceOrder?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(items.FirstOrDefault(item => item.Id == id));

        public Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => GetByIdWithDetailsAsync(id, cancellationToken);

        public Task<(IEnumerable<ServiceOrder> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? orderBy, bool orderDescending,
            IEnumerable<Expression<Func<ServiceOrder, bool>>>? filters = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IEnumerable<ServiceOrder>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<ServiceOrder>>(items);

        public Task AddAsync(ServiceOrder entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Update(ServiceOrder entity)
        {
        }

        public void Delete(ServiceOrder entity)
        {
        }
    }

    private sealed class NullCustomerRepository : ICustomerRepository
    {
        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Customer?>(null);
        public Task<IEnumerable<Customer>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Customer entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Update(Customer entity) => throw new NotSupportedException();
        public void Delete(Customer entity) => throw new NotSupportedException();
        public Task<Customer?> GetByPersonalIdAsync(string personalId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsByPersonalIdAsync(string personalId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasVehiclesAsync(Guid customerId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasServiceOrdersAsync(Guid customerId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? orderBy, bool orderDescending,
            IEnumerable<Expression<Func<Customer, bool>>>? filters = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
