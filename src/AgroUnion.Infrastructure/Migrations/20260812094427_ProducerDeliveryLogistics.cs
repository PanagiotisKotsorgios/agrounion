using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroUnion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProducerDeliveryLogistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProducerDeliveryRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProducerUserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProductionDeclarationId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ContractId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    DealId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    RouteNumber = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Product = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Variety = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QualityGrade = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LotNumber = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginAddress = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DestinationAddress = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ScheduledPickupAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LoadedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CarrierName = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DriverName = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VehiclePlate = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrailerPlate = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GrossWeight = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    TareWeight = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    NetWeight = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    RejectedWeight = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    AcceptedWeight = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    WeightUnit = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WeighedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    WeighbridgeName = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WeighingSlipNumber = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WeighingSlipUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DispatchNoteNumber = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DispatchNoteUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeliveryReceiptNumber = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeliveryReceiptUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgreementReference = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgreementType = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    QualityBonusPercent = table.Column<decimal>(type: "decimal(7,3)", nullable: false),
                    CommissionPercent = table.Column<decimal>(type: "decimal(7,3)", nullable: false),
                    WithholdingPercent = table.Column<decimal>(type: "decimal(7,3)", nullable: false),
                    VatPercent = table.Column<decimal>(type: "decimal(7,3)", nullable: false),
                    TransportCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OtherDeductions = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BaseAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BonusAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WithholdingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetPayableAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentDueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    AgreementNotes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoadNotes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InternalNotes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsVisibleToProducer = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedByUserId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProducerDeliveryRecords", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ProducerDeliveryRecords_ProducerUserId_ScheduledPickupAt",
                table: "ProducerDeliveryRecords",
                columns: new[] { "ProducerUserId", "ScheduledPickupAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProducerDeliveryRecords_ProducerUserId_Status_PaymentStatus",
                table: "ProducerDeliveryRecords",
                columns: new[] { "ProducerUserId", "Status", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_ProducerDeliveryRecords_RouteNumber",
                table: "ProducerDeliveryRecords",
                column: "RouteNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProducerDeliveryRecords");
        }
    }
}
