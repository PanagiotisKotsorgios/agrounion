using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroUnion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivatePartnerMarketplace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PartnerBuyingRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BuyerUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Product = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Unit = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaxPricePerUnit = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Region = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QualityRequirements = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValidUntilUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerBuyingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartnerBuyingRequests_AspNetUsers_BuyerUserId",
                        column: x => x.BuyerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PartnerProductionListings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProductionDeclarationId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProducerUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OfferedQuantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    AskingPricePerUnit = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerProductionListings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartnerProductionListings_AspNetUsers_ProducerUserId",
                        column: x => x.ProducerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartnerProductionListings_ProductionDeclarations_ProductionD~",
                        column: x => x.ProductionDeclarationId,
                        principalTable: "ProductionDeclarations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PartnerMarketplaceInquiries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProductionListingId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    BuyingRequestId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    SenderUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecipientUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    OfferedPricePerUnit = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Message = table.Column<string>(type: "varchar(1500)", maxLength: 1500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerMarketplaceInquiries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartnerMarketplaceInquiries_AspNetUsers_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartnerMarketplaceInquiries_AspNetUsers_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartnerMarketplaceInquiries_PartnerBuyingRequests_BuyingRequ~",
                        column: x => x.BuyingRequestId,
                        principalTable: "PartnerBuyingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartnerMarketplaceInquiries_PartnerProductionListings_Produc~",
                        column: x => x.ProductionListingId,
                        principalTable: "PartnerProductionListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerBuyingRequests_BuyerUserId",
                table: "PartnerBuyingRequests",
                column: "BuyerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerBuyingRequests_IsActive_ValidUntilUtc",
                table: "PartnerBuyingRequests",
                columns: new[] { "IsActive", "ValidUntilUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerBuyingRequests_Product_Region",
                table: "PartnerBuyingRequests",
                columns: new[] { "Product", "Region" });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerMarketplaceInquiries_BuyingRequestId",
                table: "PartnerMarketplaceInquiries",
                column: "BuyingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerMarketplaceInquiries_ProductionListingId",
                table: "PartnerMarketplaceInquiries",
                column: "ProductionListingId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerMarketplaceInquiries_RecipientUserId_Status_CreatedAt~",
                table: "PartnerMarketplaceInquiries",
                columns: new[] { "RecipientUserId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerMarketplaceInquiries_SenderUserId",
                table: "PartnerMarketplaceInquiries",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerProductionListings_IsActive_UpdatedAtUtc",
                table: "PartnerProductionListings",
                columns: new[] { "IsActive", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerProductionListings_ProducerUserId",
                table: "PartnerProductionListings",
                column: "ProducerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerProductionListings_ProductionDeclarationId",
                table: "PartnerProductionListings",
                column: "ProductionDeclarationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartnerMarketplaceInquiries");

            migrationBuilder.DropTable(
                name: "PartnerBuyingRequests");

            migrationBuilder.DropTable(
                name: "PartnerProductionListings");
        }
    }
}
