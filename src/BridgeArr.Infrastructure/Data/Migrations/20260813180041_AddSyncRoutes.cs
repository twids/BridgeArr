using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BridgeArr.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceIntegrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetIntegrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    IntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    LastQueuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncRoutes_Integrations_SourceIntegrationId",
                        column: x => x.SourceIntegrationId,
                        principalTable: "Integrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SyncRoutes_Integrations_TargetIntegrationId",
                        column: x => x.TargetIntegrationId,
                        principalTable: "Integrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncRoutes_Enabled",
                table: "SyncRoutes",
                column: "Enabled");

            migrationBuilder.CreateIndex(
                name: "IX_SyncRoutes_SourceIntegrationId_TargetIntegrationId",
                table: "SyncRoutes",
                columns: new[] { "SourceIntegrationId", "TargetIntegrationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncRoutes_TargetIntegrationId",
                table: "SyncRoutes",
                column: "TargetIntegrationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncRoutes");
        }
    }
}
