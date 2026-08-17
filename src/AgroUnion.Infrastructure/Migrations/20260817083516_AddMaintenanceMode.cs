using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroUnion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MaintenanceMessage",
                table: "PlatformConfigurations",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "MaintenanceMode",
                table: "PlatformConfigurations",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MaintenanceTitle",
                table: "PlatformConfigurations",
                type: "varchar(180)",
                maxLength: 180,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaintenanceMessage",
                table: "PlatformConfigurations");

            migrationBuilder.DropColumn(
                name: "MaintenanceMode",
                table: "PlatformConfigurations");

            migrationBuilder.DropColumn(
                name: "MaintenanceTitle",
                table: "PlatformConfigurations");
        }
    }
}
