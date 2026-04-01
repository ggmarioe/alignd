using Alignd.Application.Interfaces;
using Alignd.Infrastructure.Auth;
using Alignd.Infrastructure.Persistence;
using Alignd.Infrastructure.Persistence.Repositories;
using Alignd.Infrastructure.Profanity;
using Alignd.Infrastructure.Realtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alignd.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseNpgsql(
                config.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IRoomRepository,        RoomRepository>();
        services.AddScoped<IParticipantRepository, ParticipantRepository>();
        services.AddScoped<IVotingRoundRepository, VotingRoundRepository>();
        services.AddScoped<ITaskRepository,        TaskRepository>();

        services.AddSingleton<IProfanityFilter,    EmbeddedProfanityFilter>();
        services.AddScoped<IAdminTokenService,     AdminTokenService>();
        services.AddScoped<IParticipantTokenService, ParticipantTokenService>();
        services.AddScoped<IRoomNotifier,          SignalRRoomNotifier>();

        return services;
    }
}
