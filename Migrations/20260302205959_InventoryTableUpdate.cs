using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayAutomationService.Migrations
{
    /// <inheritdoc />
    public partial class InventoryTableUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_skus_skuId",
                table: "InventoryItems");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_skus_sku",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "Ebay_Category_Id",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "SellPrice",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "SkuCode",
                table: "InventoryItems");

            migrationBuilder.RenameColumn(
                name: "skuId",
                table: "InventoryItems",
                newName: "SkuId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryItems_skuId",
                table: "InventoryItems",
                newName: "IX_InventoryItems_SkuId");

            migrationBuilder.AlterColumn<int>(
                name: "SkuId",
                table: "InventoryItems",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_skus_SkuId",
                table: "InventoryItems",
                column: "SkuId",
                principalTable: "skus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_skus_SkuId",
                table: "InventoryItems");

            migrationBuilder.RenameColumn(
                name: "SkuId",
                table: "InventoryItems",
                newName: "skuId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryItems_SkuId",
                table: "InventoryItems",
                newName: "IX_InventoryItems_skuId");

            migrationBuilder.AlterColumn<int>(
                name: "skuId",
                table: "InventoryItems",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "Ebay_Category_Id",
                table: "InventoryItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "SellPrice",
                table: "InventoryItems",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SkuCode",
                table: "InventoryItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_skus_sku",
                table: "skus",
                column: "sku");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_skus_skuId",
                table: "InventoryItems",
                column: "skuId",
                principalTable: "skus",
                principalColumn: "Id");
        }
    }
}
