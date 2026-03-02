using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayAutomationService.Migrations
{
    /// <inheritdoc />
    public partial class uniqueConstraint : Migration
    {
        /// <inheritdoc />
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropIndex(
        name: "IX_InventoryItems_SkuId",
        table: "InventoryItems");

    migrationBuilder.CreateIndex(
        name: "IX_InventoryItems_SkuId",
        table: "InventoryItems",
        column: "SkuId",
        unique: true);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropIndex(
        name: "IX_InventoryItems_SkuId",
        table: "InventoryItems");

    migrationBuilder.CreateIndex(
        name: "IX_InventoryItems_SkuId",
        table: "InventoryItems",
        column: "SkuId");
}
    }
}
