using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InkyDesk.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "Calendars",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "Calendars");
        }
    }
}
