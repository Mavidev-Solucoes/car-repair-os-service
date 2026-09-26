using Application.Common.Interfaces;
using Application.ServiceOrders.Commands;
using Application.ServiceOrders.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("service-orders")]
public class ServiceOrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public ServiceOrdersController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceOrderCommand command, CancellationToken cancellationToken)
    {
        var userIdValidation = ValidateUserIdHeader();
        if (userIdValidation is not null)
        {
            return userIdValidation;
        }

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtRoute("GetServiceOrderById", new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}", Name = "GetServiceOrderById")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetServiceOrderByIdQuery(id), cancellationToken));

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetServiceOrdersQuery query, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(query, cancellationToken));

    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var userIdValidation = ValidateUserIdHeader();
        if (userIdValidation is not null)
        {
            return userIdValidation;
        }

        return Ok(await _mediator.Send(new CancelServiceOrderCommand(id), cancellationToken));
    }

    private IActionResult? ValidateUserIdHeader()
        => _currentUserService.UserId.HasValue ? null : BadRequest("Header 'X-User-Id' is required.");
}
