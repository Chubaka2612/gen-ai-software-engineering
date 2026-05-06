using AiTicketHub.API.Extensions;
using AiTicketHub.Application.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTicketValidator>();
builder.Services.AddFluentValidationAutoValidation();

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();

public partial class Program { }
