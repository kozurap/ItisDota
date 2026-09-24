using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItisDota.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixedMmr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Mmr",
                table: "Players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE "Players"
                SET "Mmr" = COALESCE((substring("MmRRange" from '([0-9]+)'))::integer, 0);
                """);

            migrationBuilder.DropColumn(
                name: "MmRRange",
                table: "Players");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MmRRange",
                table: "Players",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "Players"
                SET "MmRRange" = "Mmr"::text;
                """);

            migrationBuilder.DropColumn(
                name: "Mmr",
                table: "Players");
        }
    }
}
