using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayAutomationService.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferIdAndTrackingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:sku_status", "pending,inventory_created,offer_created,published,failed,rejected");

            migrationBuilder.AlterColumn<string>(
                name: "sku_status",
                table: "skus",
                type: "text",
                nullable: false,
                defaultValue: "PENDING",
                oldClrType: typeof(int),
                oldType: "sku_status",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "offer_id",
                table: "skus",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "offer_id",
                table: "skus");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:sku_status", "pending,inventory_created,offer_created,published,failed,rejected");

            migrationBuilder.AlterColumn<int>(
                name: "sku_status",
                table: "skus",
                type: "sku_status",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "PENDING");
        }
    }
}
