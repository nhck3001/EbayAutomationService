using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayAutomationService.Migrations
{
    /// <inheritdoc />
    public partial class skuNewColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "available_inventory",
                table: "skus",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "available_inventory",
                table: "skus");
        }
    }
}
