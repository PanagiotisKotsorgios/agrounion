using AgroUnion.Infrastructure;
using AgroUnion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgroUnion.Tests;

public sealed class DatabaseSeederTests
{
    [Fact]
    public async Task PasswordVersion_RotatesSeedPasswordOnlyWhenVersionChanges()
    {
        var initialPassword = "Admin1!" + new string('a', 64);
        var rotatedPassword = "Admin2!" + new string('b', 64);
        var manualPassword = "Manual1!Password";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DatabaseProvider"] = "InMemory",
            ["SeedData:AdminPassword"] = initialPassword,
            ["SeedData:DemoPassword"] = "Demo1!" + new string('c', 64),
            ["SeedData:PasswordVersion"] = "v1"
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider();

        await SeedAsync(provider);
        await using (var scope = provider.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = (await users.FindByEmailAsync("admin@agrounion.local"))!;
            Assert.True(await users.CheckPasswordAsync(admin, initialPassword));
            Assert.True((await users.ChangePasswordAsync(admin, initialPassword, manualPassword)).Succeeded);
        }

        await SeedAsync(provider);
        await using (var scope = provider.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = (await users.FindByEmailAsync("admin@agrounion.local"))!;
            Assert.True(await users.CheckPasswordAsync(admin, manualPassword));
        }

        configuration["SeedData:AdminPassword"] = rotatedPassword;
        configuration["SeedData:PasswordVersion"] = "v2";
        await SeedAsync(provider);

        await using (var scope = provider.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = (await users.FindByEmailAsync("admin@agrounion.local"))!;
            Assert.True(await users.CheckPasswordAsync(admin, rotatedPassword));
            Assert.False(await users.CheckPasswordAsync(admin, manualPassword));
        }
    }

    private static async Task SeedAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AgroUnionDbContext>();
        await db.Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
    }
}
