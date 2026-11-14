using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.ServiceOrders.Saga;

/// <summary>CloseServiceOrderCommand da saga: a OS terminou (pagamento e execução concluídos).</summary>
public sealed record CloseServiceOrderBySagaCommand(Guid ServiceOrderId) : IRequest;

/// <summary>CancelServiceOrderCommand da saga: compensação quando orçamento, pagamento ou execução falham.</summary>
public sealed record CancelServiceOrderBySagaCommand(Guid ServiceOrderId) : IRequest;

public sealed class SagaServiceOrderCommandHandlers(
    IServiceOrderRepository serviceOrderRepository,
    IUnitOfWork unitOfWork,
    ILogger<SagaServiceOrderCommandHandlers> logger)
    : IRequestHandler<CloseServiceOrderBySagaCommand>, IRequestHandler<CancelServiceOrderBySagaCommand>
{
    public Task Handle(CloseServiceOrderBySagaCommand request, CancellationToken cancellationToken)
    {
        return ApplyAsync(request.ServiceOrderId, order => order.CloseBySystem(), "closed", cancellationToken);
    }

    public Task Handle(CancelServiceOrderBySagaCommand request, CancellationToken cancellationToken)
    {
        return ApplyAsync(request.ServiceOrderId, order => order.CancelBySystem(), "cancelled", cancellationToken);
    }

    private async Task ApplyAsync(
        Guid serviceOrderId,
        Func<ServiceOrder, bool> action,
        string outcome,
        CancellationToken cancellationToken)
    {
        var serviceOrder = await serviceOrderRepository.GetByIdWithDetailsAsync(serviceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), serviceOrderId);

        if (!action(serviceOrder))
        {
            logger.LogInformation("Service order {ServiceOrderId} was already {Outcome}.", serviceOrderId, outcome);
            return;
        }

        serviceOrderRepository.Update(serviceOrder);
        await unitOfWork.CommitAsync(cancellationToken);
        logger.LogInformation("Service order {ServiceOrderId} {Outcome} by the saga.", serviceOrderId, outcome);
    }
}
