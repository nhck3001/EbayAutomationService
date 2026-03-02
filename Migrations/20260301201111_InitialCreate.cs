using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EbayAutomationService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ebay_category_name = table.Column<string>(type: "text", nullable: false),
                    ebay_category_id = table.Column<int>(type: "integer", nullable: false),
                    keywords = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                    table.UniqueConstraint("AK_categories_ebay_category_id", x => x.ebay_category_id);
                });

            migrationBuilder.CreateTable(
                name: "skus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sku = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    ebay_category_id = table.Column<int>(type: "integer", nullable: false),
                    offer_id = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    image_urls = table.Column<string[]>(type: "text[]", nullable: false),
                    item_specifics = table.Column<string>(type: "jsonb", nullable: false),
                    sell_price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    sku_status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skus", x => x.Id);
                    table.UniqueConstraint("AK_skus_sku", x => x.sku);
                });

            migrationBuilder.CreateTable(
                name: "dirtySkus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sku = table.Column<string>(type: "text", nullable: false),
                    ebay_category_id = table.Column<int>(type: "integer", nullable: false),
                    processed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dirtySkus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dirtySkus_categories_ebay_category_id",
                        column: x => x.ebay_category_id,
                        principalTable: "categories",
                        principalColumn: "ebay_category_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SkuCode = table.Column<string>(type: "text", nullable: false),
                    Ebay_Category_Id = table.Column<int>(type: "integer", nullable: false),
                    SellPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    skuId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItems_skus_skuId",
                        column: x => x.skuId,
                        principalTable: "skus",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "listings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sku = table.Column<string>(type: "text", nullable: false),
                    ebay_item_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    SkuEntityId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_listings_skus_SkuEntityId",
                        column: x => x.SkuEntityId,
                        principalTable: "skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dirtySkus_ebay_category_id",
                table: "dirtySkus",
                column: "ebay_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_dirtySkus_sku",
                table: "dirtySkus",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_skuId",
                table: "InventoryItems",
                column: "skuId");

            migrationBuilder.CreateIndex(
                name: "IX_listings_ebay_item_id",
                table: "listings",
                column: "ebay_item_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_listings_sku",
                table: "listings",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_listings_SkuEntityId",
                table: "listings",
                column: "SkuEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_skus_created_at",
                table: "skus",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_skus_sku",
                table: "skus",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_skus_sku_status",
                table: "skus",
                column: "sku_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dirtySkus");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "listings");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "skus");
        }
    }
}
