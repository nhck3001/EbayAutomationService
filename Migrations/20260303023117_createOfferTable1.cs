using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EbayAutomationService.Migrations
{
    /// <inheritdoc />
    public partial class createOfferTable1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_SkuId",
                table: "InventoryItems");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "InventoryItems",
                type: "text",
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "InventoryItems",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OfferId = table.Column<int>(type: "integer", nullable: false),
                    InventoryId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ebay_category_id = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    SellPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Offers_InventoryItems_InventoryId",
                        column: x => x.InventoryId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_CreatedAt",
                table: "InventoryItems",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_SkuId",
                table: "InventoryItems",
                column: "SkuId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Status",
                table: "InventoryItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_CreatedAt",
                table: "Offers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_InventoryId",
                table: "Offers",
                column: "InventoryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Offers_OfferId",
                table: "Offers",
                column: "OfferId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Offers_Status",
                table: "Offers",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_CreatedAt",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_SkuId",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_Status",
                table: "InventoryItems");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "InventoryItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "Pending");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "InventoryItems",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_SkuId",
                table: "InventoryItems",
                column: "SkuId");
        }
    }
}
