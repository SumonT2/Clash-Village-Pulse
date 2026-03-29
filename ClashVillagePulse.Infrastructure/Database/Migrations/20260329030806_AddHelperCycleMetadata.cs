using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClashVillagePulse.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddHelperCycleMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_static_item_images_static_item_levels_StaticItemLevelId",
                table: "static_item_images");

            migrationBuilder.AddColumn<int>(
                name: "BoostMultiplier",
                table: "static_item_levels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BoostTimeSeconds",
                table: "static_item_levels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HelperType",
                table: "static_item_levels",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_static_item_images_static_item_levels_StaticItemLevelId",
                table: "static_item_images",
                column: "StaticItemLevelId",
                principalTable: "static_item_levels",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_static_item_images_static_item_levels_StaticItemLevelId",
                table: "static_item_images");

            migrationBuilder.DropColumn(
                name: "BoostMultiplier",
                table: "static_item_levels");

            migrationBuilder.DropColumn(
                name: "BoostTimeSeconds",
                table: "static_item_levels");

            migrationBuilder.DropColumn(
                name: "HelperType",
                table: "static_item_levels");

            migrationBuilder.AddForeignKey(
                name: "FK_static_item_images_static_item_levels_StaticItemLevelId",
                table: "static_item_images",
                column: "StaticItemLevelId",
                principalTable: "static_item_levels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
