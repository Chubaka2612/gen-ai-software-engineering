// src/AiTicketHub/API/Extensions/InfrastructureServiceExtensions.cs
using AiTicketHub.Application.Interfaces;
using AiTicketHub.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace AiTicketHub.API.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<ITicketRepository, TicketRepository>();
        return services;
    }
}
