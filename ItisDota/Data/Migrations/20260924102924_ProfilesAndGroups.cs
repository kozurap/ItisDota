using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ItisDota.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProfilesAndGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Прежние строки Players пришли из JSON-импорта и не привязаны к учётной записи,
            // а KeycloakUserId обязателен и уникален.
            migrationBuilder.Sql("DELETE FROM \"Players\";");

            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropColumn(
                name: "SelectionCount",
                table: "Players");

            migrationBuilder.AddColumn<string>(
                name: "KeycloakUserId",
                table: "Players",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PlayerGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InviteToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OwnerPlayerId = table.Column<int>(type: "integer", nullable: false),
                    PromptText = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GroupMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    SelectionCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupMembers_PlayerGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "PlayerGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMembers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Players_KeycloakUserId",
                table: "Players",
                column: "KeycloakUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_GroupId_PlayerId",
                table: "GroupMembers",
                columns: new[] { "GroupId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_PlayerId",
                table: "GroupMembers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerGroups_InviteToken",
                table: "PlayerGroups",
                column: "InviteToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupMembers");

            migrationBuilder.DropTable(
                name: "PlayerGroups");

            migrationBuilder.DropIndex(
                name: "IX_Players_KeycloakUserId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "KeycloakUserId",
                table: "Players");

            migrationBuilder.AddColumn<int>(
                name: "SelectionCount",
                table: "Players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppSettings_Key",
                table: "AppSettings",
                column: "Key",
                unique: true);
        }
    }
}
