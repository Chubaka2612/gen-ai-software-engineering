using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiCraft.Banking.Services;
using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

// FluentValidation: auto-validates [FromBody] models before the action runs;
// invalid requests get a 400 ValidationProblemDetails without touching the controller.
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Register the concrete type first so DataSeeder can inject it directly,
// then forward the interface to the same singleton instance.
builder.Services.AddSingleton<TransactionService>();
builder.Services.AddSingleton<ITransactionService>(sp => sp.GetRequiredService<TransactionService>());
builder.Services.AddHostedService<DataSeeder>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "AiCraft.Banking API", Version = "v1" });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
