// src/AiTicketHub/Application/Interfaces/ITicketRepository.cs
using AiTicketHub.Domain.Common;
using AiTicketHub.Domain.Entities;

namespace AiTicketHub.Application.Interfaces;

public interface ITicketRepository
{
    Task<Result<Ticket>>                  AddAsync(Ticket ticket);
    Task<Result<Ticket>>                  GetByIdAsync(Guid id);
    Task<Result<IReadOnlyList<Ticket>>>   GetAllAsync();
    Task<Result<Ticket>>                  UpdateAsync(Ticket ticket);
    Task<Result>                          DeleteAsync(Guid id);
}
