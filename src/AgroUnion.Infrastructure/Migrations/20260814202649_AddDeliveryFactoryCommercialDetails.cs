using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroUnion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryFactoryCommercialDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FactoryName",
                table: "ProducerDeliveryRecords",
                type: "varchar(180)",
                maxLength: 180,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "FactoryUnitPrice",
                table: "ProducerDeliveryRecords",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE `ProducerDeliveryRecords` AS `delivery`
                LEFT JOIN `Deals` AS `deal` ON `deal`.`Id` = `delivery`.`DealId`
                LEFT JOIN `AspNetUsers` AS `factory` ON `factory`.`Id` = `deal`.`BuyerCounterpartyUserId`
                SET `delivery`.`FactoryName` = COALESCE(NULLIF(`factory`.`FullNameOrCompany`, ''), `delivery`.`DestinationAddress`),
                    `delivery`.`FactoryUnitPrice` = CASE
                        WHEN `deal`.`SellPricePerUnit` IS NOT NULL AND `deal`.`SellPricePerUnit` > 0 THEN `deal`.`SellPricePerUnit`
                        ELSE `delivery`.`UnitPrice`
                    END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FactoryName",
                table: "ProducerDeliveryRecords");

            migrationBuilder.DropColumn(
                name: "FactoryUnitPrice",
                table: "ProducerDeliveryRecords");
        }
    }
}
