using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddHidingFoldersSchedulesAndNoteTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "HiddenNoteTypes",
                table: "UserProfiles",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Schedules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Folders",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HiddenNoteTypes",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "Folders");
        }
    }
}
