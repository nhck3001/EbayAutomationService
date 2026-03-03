using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayAutomationService.Migrations
{
    /// <inheritdoc />
    public partial class cleanSkuTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "offer_id",
                table: "skus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "offer_id",
                table: "skus",
                type: "text",
                nullable: true);
        }
    }
}
