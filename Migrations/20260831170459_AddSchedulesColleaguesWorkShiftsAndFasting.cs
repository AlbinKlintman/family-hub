using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulesColleaguesWorkShiftsAndFasting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NoteType",
                table: "Notes",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                table: "Notes",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FastingNote_Day",
                table: "Notes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Level",
                table: "Notes",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Notes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleId",
                table: "Notes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "Notes",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "WorkShiftNote_Day",
                table: "Notes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleId",
                table: "Folders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Colleagues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Colleagues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Colleagues_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Schedules_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkShiftColleagues",
                columns: table => new
                {
                    ColleaguesId = table.Column<int>(type: "integer", nullable: false),
                    ShiftsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkShiftColleagues", x => new { x.ColleaguesId, x.ShiftsId });
                    table.ForeignKey(
                        name: "FK_WorkShiftColleagues_Colleagues_ColleaguesId",
                        column: x => x.ColleaguesId,
                        principalTable: "Colleagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkShiftColleagues_Notes_ShiftsId",
                        column: x => x.ShiftsId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notes_ScheduleId",
                table: "Notes",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_UserId_FastingNote_Day",
                table: "Notes",
                columns: new[] { "UserId", "FastingNote_Day" },
                unique: true,
                filter: "\"NoteType\" = 'Fasting'");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_ScheduleId",
                table: "Folders",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_Colleagues_UserId",
                table: "Colleagues",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_UserId",
                table: "Schedules",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkShiftColleagues_ShiftsId",
                table: "WorkShiftColleagues",
                column: "ShiftsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Folders_Schedules_ScheduleId",
                table: "Folders",
                column: "ScheduleId",
                principalTable: "Schedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Schedules_ScheduleId",
                table: "Notes",
                column: "ScheduleId",
                principalTable: "Schedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Folders_Schedules_ScheduleId",
                table: "Folders");

            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Schedules_ScheduleId",
                table: "Notes");

            migrationBuilder.DropTable(
                name: "Schedules");

            migrationBuilder.DropTable(
                name: "WorkShiftColleagues");

            migrationBuilder.DropTable(
                name: "Colleagues");

            migrationBuilder.DropIndex(
                name: "IX_Notes_ScheduleId",
                table: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_Notes_UserId_FastingNote_Day",
                table: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_Folders_ScheduleId",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "FastingNote_Day",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "ScheduleId",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "WorkShiftNote_Day",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "ScheduleId",
                table: "Folders");

            migrationBuilder.AlterColumn<string>(
                name: "NoteType",
                table: "Notes",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(13)",
                oldMaxLength: 13);
        }
    }
}
