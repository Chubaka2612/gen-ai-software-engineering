// src/AiTicketHub/API/Controllers/TicketController.cs
using AiTicketHub.Application.DTOs;
using AiTicketHub.Application.Interfaces;
using AiTicketHub.Domain.Common;
using AiTicketHub.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AiTicketHub.API.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketController : ControllerBase
{
    private readonly ITicketService _service;

    public TicketController(ITicketService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request)
    {
        var result = await _service.CreateTicketAsync(request);
        if (!result.IsSuccess) return MapError(result.Error!);
        return CreatedAtAction(nameof(GetTicketById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTicketById(Guid id)
    {
        var result = await _service.GetTicketByIdAsync(id);
        if (!result.IsSuccess) return MapError(result.Error!);
        return Ok(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> ListTickets(
        [FromQuery] TicketCategory? category,
        [FromQuery] TicketPriority? priority,
        [FromQuery] TicketStatus?   status,
        [FromQuery] string?         assignedTo,
        [FromQuery] int             page     = 1,
        [FromQuery] int             pageSize = 20)
    {
        var request = new ListTicketsRequest(category, priority, status, assignedTo, page, pageSize);
        var result  = await _service.ListTicketsAsync(request);
        if (!result.IsSuccess) return MapError(result.Error!);
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTicket(Guid id, [FromBody] UpdateTicketRequest request)
    {
        var result = await _service.UpdateTicketAsync(id, request);
        if (!result.IsSuccess) return MapError(result.Error!);
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTicket(Guid id)
    {
        var result = await _service.DeleteTicketAsync(id);
        if (!result.IsSuccess) return MapError(result.Error!);
        return NoContent();
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "Ticket.NotFound"      => NotFound(error),
        "Ticket.InvalidStatus" => UnprocessableEntity(error),
        "Ticket.Duplicate"     => Conflict(error),
        "Validation.Failed"    => BadRequest(error),
        _                      => StatusCode(500, error)
    };
}
