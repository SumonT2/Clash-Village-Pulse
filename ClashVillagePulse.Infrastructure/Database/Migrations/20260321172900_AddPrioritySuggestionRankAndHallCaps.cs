using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClashVillagePulse.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPrioritySuggestionRankAndHallCaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StaticDataRunSteps_StaticDataRuns_StaticDataRunId",
                table: "StaticDataRunSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_StaticItemLevels_StaticItems_StaticItemId",
                table: "StaticItemLevels");

            migrationBuilder.DropForeignKey(
                name: "FK_StaticItemLevelUpgradeCosts_StaticItemLevels_StaticItemLeve~",
                table: "StaticItemLevelUpgradeCosts");

            migrationBuilder.DropForeignKey(
                name: "FK_StaticItemRequirements_StaticItemLevels_StaticItemLevelId",
                table: "StaticItemRequirements");

            migrationBuilder.DropIndex(
                name: "IX_priority_suggestions_VillageId",
                table: "priority_suggestions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StaticItems",
                table: "StaticItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StaticItemRequirements",
                table: "StaticItemRequirements");

            migrationBuilder.DropIndex(
                name: "IX_StaticItemRequirements_StaticItemLevelId",
                table: "StaticItemRequirements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StaticItemLevelUpgradeCosts",
                table: "StaticItemLevelUpgradeCosts");

            migrationBuilder.DropIndex(
                name: "IX_StaticItemLevelUpgradeCosts_StaticItemLevelId",
                table: "StaticItemLevelUpgradeCosts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StaticItemLevels",
                table: "StaticItemLevels");

            migrationBuilder.DropIndex(
                name: "IX_StaticItemLevels_StaticItemId",
                table: "StaticItemLevels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StaticDataRunSteps",
                table: "StaticDataRunSteps");

            migrationBuilder.DropIndex(
                name: "IX_StaticDataRunSteps_StaticDataRunId",
                table: "StaticDataRunSteps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StaticDataRuns",
                table: "StaticDataRuns");

            migrationBuilder.RenameTable(
                name: "StaticItems",
                newName: "static_items");

            migrationBuilder.RenameTable(
                name: "StaticItemRequirements",
                newName: "static_item_requirements");

            migrationBuilder.RenameTable(
                name: "StaticItemLevelUpgradeCosts",
                newName: "static_item_level_upgrade_costs");

            migrationBuilder.RenameTable(
                name: "StaticItemLevels",
                newName: "static_item_levels");

            migrationBuilder.RenameTable(
                name: "StaticDataRunSteps",
                newName: "static_data_run_steps");

            migrationBuilder.RenameTable(
                name: "StaticDataRuns",
                newName: "static_data_runs");

            migrationBuilder.AddColumn<int>(
                name: "SuggestedPriorityRank",
                table: "priority_suggestions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "static_items",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "TargetKey",
                table: "static_data_run_steps",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "static_data_run_steps",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RequestedByUserId",
                table: "static_data_runs",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "static_data_runs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Fingerprint",
                table: "static_data_runs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_static_items",
                table: "static_items",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_static_item_requirements",
                table: "static_item_requirements",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_static_item_level_upgrade_costs",
                table: "static_item_level_upgrade_costs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_static_item_levels",
                table: "static_item_levels",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_static_data_run_steps",
                table: "static_data_run_steps",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_static_data_runs",
                table: "static_data_runs",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "static_hall_item_caps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Section = table.Column<int>(type: "integer", nullable: false),
                    HallLevel = table.Column<int>(type: "integer", nullable: false),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    ItemDataId = table.Column<int>(type: "integer", nullable: false),
                    MaxCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_static_hall_item_caps", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_priority_suggestions_VillageId_Section_ItemType_ItemDataId",
                table: "priority_suggestions",
                columns: new[] { "VillageId", "Section", "ItemType", "ItemDataId" });

            migrationBuilder.CreateIndex(
                name: "IX_priority_suggestions_VillageId_Status_CreatedAtUtc",
                table: "priority_suggestions",
                columns: new[] { "VillageId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_static_items_ItemDataId_ItemType_Section",
                table: "static_items",
                columns: new[] { "ItemDataId", "ItemType", "Section" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_static_item_requirements_StaticItemLevelId_RequirementType_~",
                table: "static_item_requirements",
                columns: new[] { "StaticItemLevelId", "RequirementType", "RequiredItemDataId", "RequiredLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_static_item_level_upgrade_costs_StaticItemLevelId_ResourceT~",
                table: "static_item_level_upgrade_costs",
                columns: new[] { "StaticItemLevelId", "ResourceType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_static_item_levels_StaticItemId_Level",
                table: "static_item_levels",
                columns: new[] { "StaticItemId", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_static_data_run_steps_StaticDataRunId_TargetKey_StepType",
                table: "static_data_run_steps",
                columns: new[] { "StaticDataRunId", "TargetKey", "StepType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_static_data_run_steps_Status",
                table: "static_data_run_steps",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_static_data_runs_RequestedAtUtc",
                table: "static_data_runs",
                column: "RequestedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_static_data_runs_Status",
                table: "static_data_runs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_static_hall_item_caps_Section_HallLevel_ItemType_ItemDataId",
                table: "static_hall_item_caps",
                columns: new[] { "Section", "HallLevel", "ItemType", "ItemDataId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_static_data_run_steps_static_data_runs_StaticDataRunId",
                table: "static_data_run_steps",
                column: "StaticDataRunId",
                principalTable: "static_data_runs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_static_item_level_upgrade_costs_static_item_levels_StaticIt~",
                table: "static_item_level_upgrade_costs",
                column: "StaticItemLevelId",
                principalTable: "static_item_levels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_static_item_levels_static_items_StaticItemId",
                table: "static_item_levels",
                column: "StaticItemId",
                principalTable: "static_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_static_item_requirements_static_item_levels_StaticItemLevel~",
                table: "static_item_requirements",
                column: "StaticItemLevelId",
                principalTable: "static_item_levels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_static_data_run_steps_static_data_runs_StaticDataRunId",
                table: "static_data_run_steps");

            migrationBuilder.DropForeignKey(
                name: "FK_static_item_level_upgrade_costs_static_item_levels_StaticIt~",
                table: "static_item_level_upgrade_costs");

            migrationBuilder.DropForeignKey(
                name: "FK_static_item_levels_static_items_StaticItemId",
                table: "static_item_levels");

            migrationBuilder.DropForeignKey(
                name: "FK_static_item_requirements_static_item_levels_StaticItemLevel~",
                table: "static_item_requirements");

            migrationBuilder.DropTable(
                name: "static_hall_item_caps");

            migrationBuilder.DropIndex(
                name: "IX_priority_suggestions_VillageId_Section_ItemType_ItemDataId",
                table: "priority_suggestions");

            migrationBuilder.DropIndex(
                name: "IX_priority_suggestions_VillageId_Status_CreatedAtUtc",
                table: "priority_suggestions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_static_items",
                table: "static_items");

            migrationBuilder.DropIndex(
                name: "IX_static_items_ItemDataId_ItemType_Section",
                table: "static_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_static_item_requirements",
                table: "static_item_requirements");

            migrationBuilder.DropIndex(
                name: "IX_static_item_requirements_StaticItemLevelId_RequirementType_~",
                table: "static_item_requirements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_static_item_levels",
                table: "static_item_levels");

            migrationBuilder.DropIndex(
                name: "IX_static_item_levels_StaticItemId_Level",
                table: "static_item_levels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_static_item_level_upgrade_costs",
                table: "static_item_level_upgrade_costs");

            migrationBuilder.DropIndex(
                name: "IX_static_item_level_upgrade_costs_StaticItemLevelId_ResourceT~",
                table: "static_item_level_upgrade_costs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_static_data_runs",
                table: "static_data_runs");

            migrationBuilder.DropIndex(
                name: "IX_static_data_runs_RequestedAtUtc",
                table: "static_data_runs");

            migrationBuilder.DropIndex(
                name: "IX_static_data_runs_Status",
                table: "static_data_runs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_static_data_run_steps",
                table: "static_data_run_steps");

            migrationBuilder.DropIndex(
                name: "IX_static_data_run_steps_StaticDataRunId_TargetKey_StepType",
                table: "static_data_run_steps");

            migrationBuilder.DropIndex(
                name: "IX_static_data_run_steps_Status",
                table: "static_data_run_steps");

            migrationBuilder.DropColumn(
                name: "SuggestedPriorityRank",
                table: "priority_suggestions");

            migrationBuilder.RenameTable(
                name: "static_items",
                newName: "StaticItems");

            migrationBuilder.RenameTable(
                name: "static_item_requirements",
                newName: "StaticItemRequirements");

            migrationBuilder.RenameTable(
                name: "static_item_levels",
                newName: "StaticItemLevels");

            migrationBuilder.RenameTable(
                name: "static_item_level_upgrade_costs",
                newName: "StaticItemLevelUpgradeCosts");

            migrationBuilder.RenameTable(
                name: "static_data_runs",
                newName: "StaticDataRuns");

            migrationBuilder.RenameTable(
                name: "static_data_run_steps",
                newName: "StaticDataRunSteps");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "StaticItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "RequestedByUserId",
                table: "StaticDataRuns",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "StaticDataRuns",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Fingerprint",
                table: "StaticDataRuns",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "TargetKey",
                table: "StaticDataRunSteps",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "StaticDataRunSteps",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_StaticItems",
                table: "StaticItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StaticItemRequirements",
                table: "StaticItemRequirements",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StaticItemLevels",
                table: "StaticItemLevels",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StaticItemLevelUpgradeCosts",
                table: "StaticItemLevelUpgradeCosts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StaticDataRuns",
                table: "StaticDataRuns",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StaticDataRunSteps",
                table: "StaticDataRunSteps",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_priority_suggestions_VillageId",
                table: "priority_suggestions",
                column: "VillageId");

            migrationBuilder.CreateIndex(
                name: "IX_StaticItemRequirements_StaticItemLevelId",
                table: "StaticItemRequirements",
                column: "StaticItemLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_StaticItemLevels_StaticItemId",
                table: "StaticItemLevels",
                column: "StaticItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StaticItemLevelUpgradeCosts_StaticItemLevelId",
                table: "StaticItemLevelUpgradeCosts",
                column: "StaticItemLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_StaticDataRunSteps_StaticDataRunId",
                table: "StaticDataRunSteps",
                column: "StaticDataRunId");

            migrationBuilder.AddForeignKey(
                name: "FK_StaticDataRunSteps_StaticDataRuns_StaticDataRunId",
                table: "StaticDataRunSteps",
                column: "StaticDataRunId",
                principalTable: "StaticDataRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StaticItemLevels_StaticItems_StaticItemId",
                table: "StaticItemLevels",
                column: "StaticItemId",
                principalTable: "StaticItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StaticItemLevelUpgradeCosts_StaticItemLevels_StaticItemLeve~",
                table: "StaticItemLevelUpgradeCosts",
                column: "StaticItemLevelId",
                principalTable: "StaticItemLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StaticItemRequirements_StaticItemLevels_StaticItemLevelId",
                table: "StaticItemRequirements",
                column: "StaticItemLevelId",
                principalTable: "StaticItemLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
