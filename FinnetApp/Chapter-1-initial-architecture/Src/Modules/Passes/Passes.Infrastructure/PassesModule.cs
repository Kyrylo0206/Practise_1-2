namespace EvolutionaryArchitecture.Fitnet.Passes.Infrastructure;

using Application;
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public static class PassesModule
{
    public static IServiceCollection AddPassesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PassesPersistenceOptions>(configuration.GetSection(PassesPersistenceOptions.SectionName));
        services.AddOptionsWithValidateOnStart<PassesPersistenceOptions>();
        services.AddDbContext<PassesPersistence>((serviceProvider, options) =>
        {
            var persistenceOptions = serviceProvider.GetRequiredService<IOptions<PassesPersistenceOptions>>();
            var connectionString = persistenceOptions.Value.Passes;
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IPassRepository, PassRepository>();
        services.AddScoped<IPassService, PassService>();

        return services;
    }

    public static void UsePassesModule(this Microsoft.AspNetCore.Builder.IApplicationBuilder applicationBuilder)
    {
        using var scope = applicationBuilder.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PassesPersistence>();
        context.Database.EnsureCreated();
    }
}
