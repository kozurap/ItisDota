using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItisDota.Data.Migrations
{
    /// <inheritdoc />
    public partial class SteamFriendId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SteamFriendId",
                table: "Players",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SteamFriendId",
                table: "Players");
        }
    }
}
