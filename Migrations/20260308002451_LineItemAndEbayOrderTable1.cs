using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayAutomationService.Migrations
{
    /// <inheritdoc />
    public partial class LineItemAndEbayOrderTable1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_order_items_orders_order_id",
                table: "order_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_order_items",
                table: "order_items");

            migrationBuilder.RenameTable(
                name: "order_items",
                newName: "line_items");

            migrationBuilder.RenameIndex(
                name: "IX_order_items_order_id",
                table: "line_items",
                newName: "IX_line_items_order_id");

            migrationBuilder.RenameIndex(
                name: "IX_order_items_line_item_id",
                table: "line_items",
                newName: "IX_line_items_line_item_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_line_items",
                table: "line_items",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_line_items_orders_order_id",
                table: "line_items",
                column: "order_id",
                principalTable: "orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_line_items_orders_order_id",
                table: "line_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_line_items",
                table: "line_items");

            migrationBuilder.RenameTable(
                name: "line_items",
                newName: "order_items");

            migrationBuilder.RenameIndex(
                name: "IX_line_items_order_id",
                table: "order_items",
                newName: "IX_order_items_order_id");

            migrationBuilder.RenameIndex(
                name: "IX_line_items_line_item_id",
                table: "order_items",
                newName: "IX_order_items_line_item_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_order_items",
                table: "order_items",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_orders_order_id",
                table: "order_items",
                column: "order_id",
                principalTable: "orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
