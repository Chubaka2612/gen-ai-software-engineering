// src/AiTicketHub/Domain/Entities/Ticket.cs
using AiTicketHub.Domain.Enums;

namespace AiTicketHub.Domain.Entities;

public class Ticket
{
    public Guid Id { get; private set; }
    public string CustomerId { get; private set; } = default!;
    public string CustomerEmail { get; private set; } = default!;
    public string CustomerName { get; private set; } = default!;
    public string Subject { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public TicketCategory Category { get; private set; }
    public TicketPriority Priority { get; private set; }
    public TicketStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? AssignedTo { get; private set; }
    public List<string> Tags { get; private set; } = default!;
    public TicketSource Source { get; private set; }
    public string? Browser { get; private set; }
    public DeviceType DeviceType { get; private set; }

    public Ticket(
        Guid id,
        string customerId,
        string customerEmail,
        string customerName,
        string subject,
        string description,
        TicketCategory category,
        TicketPriority priority,
        TicketStatus status,
        DateTime createdAt,
        DateTime updatedAt,
        DateTime? resolvedAt,
        string? assignedTo,
        List<string> tags,
        TicketSource source,
        string? browser,
        DeviceType deviceType)
    { }
}
