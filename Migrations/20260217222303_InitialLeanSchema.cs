using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayAutomationService.Migrations
{
    /// <inheritdoc />
    public partial class InitialLeanSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_product_pids_categories_category_id",
                table: "product_pids");

            migrationBuilder.DropPrimaryKey(
                name: "PK_product_pids",
                table: "product_pids");

            migrationBuilder.RenameTable(
                name: "product_pids",
                newName: "dirtySkus");

            migrationBuilder.RenameColumn(
                name: "category_id",
                table: "dirtySkus",
                newName: "ebay_category_id");

            migrationBuilder.RenameIndex(
                name: "IX_product_pids_sku",
                table: "dirtySkus",
                newName: "IX_dirtySkus_sku");

            migrationBuilder.RenameIndex(
                name: "IX_product_pids_category_id",
                table: "dirtySkus",
                newName: "IX_dirtySkus_ebay_category_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_dirtySkus",
                table: "dirtySkus",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_dirtySkus_categories_ebay_category_id",
                table: "dirtySkus",
                column: "ebay_category_id",
                principalTable: "categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_dirtySkus_categories_ebay_category_id",
                table: "dirtySkus");

            migrationBuilder.DropPrimaryKey(
                name: "PK_dirtySkus",
                table: "dirtySkus");

            migrationBuilder.RenameTable(
                name: "dirtySkus",
                newName: "product_pids");

            migrationBuilder.RenameColumn(
                name: "ebay_category_id",
                table: "product_pids",
                newName: "category_id");

            migrationBuilder.RenameIndex(
                name: "IX_dirtySkus_sku",
                table: "product_pids",
                newName: "IX_product_pids_sku");

            migrationBuilder.RenameIndex(
                name: "IX_dirtySkus_ebay_category_id",
                table: "product_pids",
                newName: "IX_product_pids_category_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_product_pids",
                table: "product_pids",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_product_pids_categories_category_id",
                table: "product_pids",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
