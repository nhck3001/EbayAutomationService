using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EbayAutomationService.Migrations
{
    /// <inheritdoc />
    public partial class AddDirtySkus : Migration
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
                    ebay_category_id = table.Column<string>(type: "text", nullable: false),
                    keyword = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "skus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sku = table.Column<string>(type: "text", nullable: false),
                    pid = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    image_urls = table.Column<string[]>(type: "text[]", nullable: false),
                    item_specifics = table.Column<string>(type: "jsonb", nullable: false),
                    sell_price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    processed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skus", x => x.Id);
                    table.UniqueConstraint("AK_skus_sku", x => x.sku);
                });

            migrationBuilder.CreateTable(
                name: "product_pids",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sku = table.Column<string>(type: "text", nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: false),
                    processed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_pids", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_pids_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "listings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sku = table.Column<string>(type: "text", nullable: false),
                    ebay_item_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_listings_skus_sku",
                        column: x => x.sku,
                        principalTable: "skus",
                        principalColumn: "sku",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_categories_ebay_category_id",
                table: "categories",
                column: "ebay_category_id",
                unique: true);

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
                name: "IX_product_pids_category_id",
                table: "product_pids",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_pids_sku",
                table: "product_pids",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_skus_created_at",
                table: "skus",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_skus_processed",
                table: "skus",
                column: "processed");

            migrationBuilder.CreateIndex(
                name: "IX_skus_sku",
                table: "skus",
                column: "sku",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "listings");

            migrationBuilder.DropTable(
                name: "product_pids");

            migrationBuilder.DropTable(
                name: "skus");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
