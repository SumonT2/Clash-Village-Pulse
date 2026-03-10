using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClashVillagePulse.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddVillageUploadFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_village_item_levels_VillageId_Section_ItemType_ItemDataId",
                table: "village_item_levels");

            migrationBuilder.CreateIndex(
                name: "IX_village_item_levels_VillageId_Section_ItemType_ItemDataId_L~",
                table: "village_item_levels",
                columns: new[] { "VillageId", "Section", "ItemType", "ItemDataId", "Level" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_village_item_levels_VillageId_Section_ItemType_ItemDataId_L~",
                table: "village_item_levels");

            migrationBuilder.CreateIndex(
                name: "IX_village_item_levels_VillageId_Section_ItemType_ItemDataId",
                table: "village_item_levels",
                columns: new[] { "VillageId", "Section", "ItemType", "ItemDataId" });
        }
    }
}
