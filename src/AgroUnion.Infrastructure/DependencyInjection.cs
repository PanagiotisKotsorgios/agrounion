using AgroUnion.Application.Services;
using AgroUnion.Infrastructure.Persistence;
using AgroUnion.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgroUnion.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["DatabaseProvider"] ?? "InMemory";
        services.AddDbContext<AgroUnionDbContext>(options =>
        {
            if (provider.Equals("MySql", StringComparison.OrdinalIgnoreCase))
            {
                var connection = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Λείπει το connection string DefaultConnection.");
                options.UseMySql(connection, new MySqlServerVersion(new Version(8, 0, 36)), mysql => mysql.MigrationsAssembly(typeof(AgroUnionDbContext).Assembly.FullName));
            }
            else options.UseInMemoryDatabase("AgroUnionDemo");
        });

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 10;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.User.RequireUniqueEmail = true;
        }).AddEntityFrameworkStores<AgroUnionDbContext>().AddDefaultTokenProviders();

        services.AddScoped<IAgroUnionService, AgroUnionService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        if (configuration.GetValue("Smtp:Enabled", false)) services.AddScoped<IEmailSender, SmtpEmailSender>();
        else services.AddScoped<IEmailSender, DevelopmentEmailSender>();
        services.AddScoped<DatabaseSeeder>();
        return services;
    }
}
