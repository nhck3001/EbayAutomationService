using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayAutomationService.Migrations
{
    /// <inheritdoc />
    public partial class listingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_listings_skus_SkuEntityId",
                table: "listings");

            migrationBuilder.DropIndex(
                name: "IX_listings_ebay_item_id",
                table: "listings");

            migrationBuilder.DropIndex(
                name: "IX_listings_SkuEntityId",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "ebay_item_id",
                table: "listings");

            migrationBuilder.RenameColumn(
                name: "sku",
                table: "listings",
                newName: "ListingId");

            migrationBuilder.RenameColumn(
                name: "SkuEntityId",
                table: "listings",
                newName: "OfferId");

            migrationBuilder.RenameIndex(
                name: "IX_listings_sku",
                table: "listings",
                newName: "IX_listings_ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_listings_OfferId",
                table: "listings",
                column: "OfferId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_listings_Offers_OfferId",
                table: "listings",
                column: "OfferId",
                principalTable: "Offers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_listings_Offers_OfferId",
                table: "listings");

            migrationBuilder.DropIndex(
                name: "IX_listings_OfferId",
                table: "listings");

            migrationBuilder.RenameColumn(
                name: "OfferId",
                table: "listings",
                newName: "SkuEntityId");

            migrationBuilder.RenameColumn(
                name: "ListingId",
                table: "listings",
                newName: "sku");

            migrationBuilder.RenameIndex(
                name: "IX_listings_ListingId",
                table: "listings",
                newName: "IX_listings_sku");

            migrationBuilder.AddColumn<string>(
                name: "ebay_item_id",
                table: "listings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_listings_ebay_item_id",
                table: "listings",
                column: "ebay_item_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_listings_SkuEntityId",
                table: "listings",
                column: "SkuEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_listings_skus_SkuEntityId",
                table: "listings",
                column: "SkuEntityId",
                principalTable: "skus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
