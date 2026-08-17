using AgroUnion.Domain.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace AgroUnion.Infrastructure.Persistence;

// Production seeder: creates the four Identity roles and a single administrator account.
// Every other record (producers, buyers, applications, deals, contracts, invoices, delivery records,
// contact messages, collaboration profiles, documents, offers, price list, supply orders, etc.)
// is intentionally NOT seeded — the admin portal must reflect only real activity from real users:
// partners register via /apply, get approved, and their actions populate the data.
public sealed class DatabaseSeeder(AgroUnionDbContext db, UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles, IConfiguration configuration)
{
    private const string SeedPasswordVersionClaim = "agrounion:seed-password-version";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        _ = db; // context resolved so migrations complete; no seed rows are written.

        foreach (var role in RoleNames.All)
            if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole(role));

        var adminPassword = configuration["SeedData:AdminPassword"] ?? "Admin!2026Demo";
        var passwordVersion = configuration["SeedData:PasswordVersion"];
        await EnsureAdminAsync(adminPassword, passwordVersion);
    }

    private async Task EnsureAdminAsync(string password, string? passwordVersion)
    {
        const string email = "admin@agrounion.local";
        var user = await users.FindByEmailAsync(email);
        var created = user is null;
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullNameOrCompany = "Διαχειριστής AGRO UNION",
                Region = "Μεσολόγγι"
            };
            EnsureSucceeded(await users.CreateAsync(user, password));
        }

        if (!string.IsNullOrWhiteSpace(passwordVersion))
        {
            var claims = await users.GetClaimsAsync(user);
            var versionClaim = claims.SingleOrDefault(x => x.Type == SeedPasswordVersionClaim);
            if (!created && versionClaim?.Value != passwordVersion)
            {
                var resetToken = await users.GeneratePasswordResetTokenAsync(user);
                EnsureSucceeded(await users.ResetPasswordAsync(user, resetToken, password));
            }

            if (versionClaim is null)
                EnsureSucceeded(await users.AddClaimAsync(user, new Claim(SeedPasswordVersionClaim, passwordVersion)));
            else if (versionClaim.Value != passwordVersion)
                EnsureSucceeded(await users.ReplaceClaimAsync(user, versionClaim, new Claim(SeedPasswordVersionClaim, passwordVersion)));
        }

        if (!await users.IsInRoleAsync(user, RoleNames.Admin))
            EnsureSucceeded(await users.AddToRoleAsync(user, RoleNames.Admin));
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(" ", result.Errors.Select(x => x.Description)));
    }
}
