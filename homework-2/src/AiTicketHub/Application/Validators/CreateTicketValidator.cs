// src/AiTicketHub/Application/Validators/CreateTicketValidator.cs
using AiTicketHub.Application.DTOs;
using FluentValidation;

namespace AiTicketHub.Application.Validators;

public class CreateTicketValidator : AbstractValidator<CreateTicketRequest>
{
    public CreateTicketValidator() { }
}
