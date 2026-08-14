using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgroUnion.Infrastructure.Persistence;

public sealed class AgroUnionDbContextFactory : IDesignTimeDbContextFactory<AgroUnionDbContext>
{
    public AgroUnionDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "server=localhost;port=3306;database=agro_union;user=agro;password=design-time-only";
        var options = new DbContextOptionsBuilder<AgroUnionDbContext>()
            .UseMySql(connection, new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;
        return new AgroUnionDbContext(options);
    }
}
