using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddFoldersPriorityAndReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FolderId",
                table: "Notes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Notes",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Reminder1hSentAtUtc",
                table: "Notes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Reminder24hSentAtUtc",
                table: "Notes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InterviewReminder1hSentAtUtc",
                table: "JobApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InterviewReminder24hSentAtUtc",
                table: "JobApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "InterviewTime",
                table: "JobApplications",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Folders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ParentFolderId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Folders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Folders_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Folders_Folders_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalTable: "Folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notes_FolderId",
                table: "Notes",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_ParentFolderId",
                table: "Folders",
                column: "ParentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_UserId",
                table: "Folders",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Folders_FolderId",
                table: "Notes",
                column: "FolderId",
                principalTable: "Folders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Folders_FolderId",
                table: "Notes");

            migrationBuilder.DropTable(
                name: "Folders");

            migrationBuilder.DropIndex(
                name: "IX_Notes_FolderId",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "Reminder1hSentAtUtc",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "Reminder24hSentAtUtc",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "InterviewReminder1hSentAtUtc",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "InterviewReminder24hSentAtUtc",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "InterviewTime",
                table: "JobApplications");
        }
    }
}
