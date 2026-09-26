using Api.Requests;
using Application.ServiceOrders.Commands;
using Application.ServiceOrders.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/service-orders")]
public class ServiceOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public ServiceOrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetServiceOrdersQuery query, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetServiceOrderByIdQuery(id), cancellationToken));

    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetServiceStatusHistoryQuery(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Open([FromBody] OpenServiceOrderCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/items")]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] AddServiceOrderItemRequest request, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new AddServiceOrderItemCommand(id, request.Description, request.UnitPrice, request.Quantity), cancellationToken));

    [HttpPut("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> UpdateItem(Guid id, Guid itemId, [FromBody] UpdateServiceOrderItemRequest request, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new UpdateServiceOrderItemCommand(id, itemId, request.Description, request.UnitPrice, request.Quantity), cancellationToken));

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid id, Guid itemId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new RemoveServiceOrderItemCommand(id, itemId), cancellationToken));

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateServiceOrderStatusRequest request, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new UpdateServiceOrderStatusCommand(id, request.Status), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteServiceOrderCommand(id), cancellationToken);
        return NoContent();
    }
}
