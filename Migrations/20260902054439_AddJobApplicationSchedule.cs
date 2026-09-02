using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddJobApplicationSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ScheduleId",
                table: "JobApplications",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_ScheduleId",
                table: "JobApplications",
                column: "ScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobApplications_Schedules_ScheduleId",
                table: "JobApplications",
                column: "ScheduleId",
                principalTable: "Schedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobApplications_Schedules_ScheduleId",
                table: "JobApplications");

            migrationBuilder.DropIndex(
                name: "IX_JobApplications_ScheduleId",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "ScheduleId",
                table: "JobApplications");
        }
    }
}
