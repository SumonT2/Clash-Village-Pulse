using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClashVillagePulse.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddFankitImageCrawler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "static_image_assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SourcePageUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LocalPath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DownloadedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MatchScore = table.Column<double>(type: "double precision", nullable: false),
                    MatchReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_static_image_assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "static_item_images",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaticItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    StaticItemLevelId = table.Column<Guid>(type: "uuid", nullable: true),
                    StaticImageAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetKind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MatchedLevel = table.Column<int>(type: "integer", nullable: true),
                    IsPreferred = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_static_item_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_static_item_images_static_image_assets_StaticImageAssetId",
                        column: x => x.StaticImageAssetId,
                        principalTable: "static_image_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_static_item_images_static_item_levels_StaticItemLevelId",
                        column: x => x.StaticItemLevelId,
                        principalTable: "static_item_levels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_static_item_images_static_items_StaticItemId",
                        column: x => x.StaticItemId,
                        principalTable: "static_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_static_image_assets_LocalPath",
                table: "static_image_assets",
                column: "LocalPath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_static_image_assets_SourceType_SourceUrl",
                table: "static_image_assets",
                columns: new[] { "SourceType", "SourceUrl" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_static_item_images_StaticImageAssetId",
                table: "static_item_images",
                column: "StaticImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_static_item_images_StaticItemId_StaticItemLevelId_IsPreferr~",
                table: "static_item_images",
                columns: new[] { "StaticItemId", "StaticItemLevelId", "IsPreferred" });

            migrationBuilder.CreateIndex(
                name: "IX_static_item_images_StaticItemId_StaticItemLevelId_StaticIma~",
                table: "static_item_images",
                columns: new[] { "StaticItemId", "StaticItemLevelId", "StaticImageAssetId", "AssetKind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_static_item_images_StaticItemLevelId",
                table: "static_item_images",
                column: "StaticItemLevelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "static_item_images");

            migrationBuilder.DropTable(
                name: "static_image_assets");
        }
    }
}
