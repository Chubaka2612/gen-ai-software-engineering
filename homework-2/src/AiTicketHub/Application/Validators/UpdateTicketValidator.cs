// src/AiTicketHub/Application/Validators/UpdateTicketValidator.cs
using AiTicketHub.Application.DTOs;
using FluentValidation;

namespace AiTicketHub.Application.Validators;

public class UpdateTicketValidator : AbstractValidator<UpdateTicketRequest>
{
    public UpdateTicketValidator() { }
}
