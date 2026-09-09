using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseSeatPositionSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SeatForwardPosition",
                table: "Exercises",
                type: "numeric(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SeatHeightPosition",
                table: "Exercises",
                type: "numeric(6,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeatForwardPosition",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "SeatHeightPosition",
                table: "Exercises");
        }
    }
}
