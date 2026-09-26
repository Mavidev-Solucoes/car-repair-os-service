using Application.Vehicles.Commands;
using Application.Vehicles.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/vehicles")]
public class VehiclesController : ControllerBase
{
    private readonly IMediator _mediator;

    public VehiclesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetVehiclesQuery query, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetVehicleByIdQuery(id), cancellationToken));

    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> GetByCustomerId(Guid customerId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetVehiclesByCustomerIdQuery(customerId), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicleCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest("Route ID and body ID do not match.");
        }

        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteVehicleCommand(id), cancellationToken);
        return NoContent();
    }
}
