using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClashVillagePulse.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class StaticDataFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StaticDataRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Fingerprint = table.Column<string>(type: "text", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "text", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaticDataRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaticItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemDataId = table.Column<int>(type: "integer", nullable: false),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Section = table.Column<int>(type: "integer", nullable: false),
                    IsUpgradeable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaticItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaticDataRunSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaticDataRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetKey = table.Column<string>(type: "text", nullable: false),
                    StepType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaticDataRunSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaticDataRunSteps_StaticDataRuns_StaticDataRunId",
                        column: x => x.StaticDataRunId,
                        principalTable: "StaticDataRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StaticItemLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaticItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    UpgradeTimeSeconds = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaticItemLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaticItemLevels_StaticItems_StaticItemId",
                        column: x => x.StaticItemId,
                        principalTable: "StaticItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StaticItemLevelUpgradeCosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaticItemLevelId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaticItemLevelUpgradeCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaticItemLevelUpgradeCosts_StaticItemLevels_StaticItemLeve~",
                        column: x => x.StaticItemLevelId,
                        principalTable: "StaticItemLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StaticItemRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaticItemLevelId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequirementType = table.Column<int>(type: "integer", nullable: false),
                    RequiredItemDataId = table.Column<int>(type: "integer", nullable: true),
                    RequiredLevel = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaticItemRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaticItemRequirements_StaticItemLevels_StaticItemLevelId",
                        column: x => x.StaticItemLevelId,
                        principalTable: "StaticItemLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaticDataRunSteps_StaticDataRunId",
                table: "StaticDataRunSteps",
                column: "StaticDataRunId");

            migrationBuilder.CreateIndex(
                name: "IX_StaticItemLevels_StaticItemId",
                table: "StaticItemLevels",
                column: "StaticItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StaticItemLevelUpgradeCosts_StaticItemLevelId",
                table: "StaticItemLevelUpgradeCosts",
                column: "StaticItemLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_StaticItemRequirements_StaticItemLevelId",
                table: "StaticItemRequirements",
                column: "StaticItemLevelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaticDataRunSteps");

            migrationBuilder.DropTable(
                name: "StaticItemLevelUpgradeCosts");

            migrationBuilder.DropTable(
                name: "StaticItemRequirements");

            migrationBuilder.DropTable(
                name: "StaticDataRuns");

            migrationBuilder.DropTable(
                name: "StaticItemLevels");

            migrationBuilder.DropTable(
                name: "StaticItems");
        }
    }
}
