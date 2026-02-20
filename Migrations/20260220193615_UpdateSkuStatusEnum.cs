using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayAutomationService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSkuStatusEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_skus_processed",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "processed",
                table: "skus");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:sku_status", "pending,inventory_created,offer_created,published,failed,rejected");

            migrationBuilder.AddColumn<int>(
                name: "sku_status",
                table: "skus",
                type: "sku_status",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_skus_sku_status",
                table: "skus",
                column: "sku_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_skus_sku_status",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "sku_status",
                table: "skus");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:sku_status", "pending,inventory_created,offer_created,published,failed,rejected");

            migrationBuilder.AddColumn<bool>(
                name: "processed",
                table: "skus",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_skus_processed",
                table: "skus",
                column: "processed");
        }
    }
}
