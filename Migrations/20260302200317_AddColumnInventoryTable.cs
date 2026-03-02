using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayAutomationService.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnInventoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvailableInventory",
                table: "InventoryItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableInventory",
                table: "InventoryItems");
        }
    }
}
