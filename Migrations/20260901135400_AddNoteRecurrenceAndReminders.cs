using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteRecurrenceAndReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reminder1hSentAtUtc",
                table: "Notes");

            migrationBuilder.AddColumn<string>(
                name: "RecurrenceIntervalUnit",
                table: "Notes",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceIntervalValue",
                table: "Notes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NoteReminder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NoteId = table.Column<int>(type: "integer", nullable: false),
                    OffsetValue = table.Column<int>(type: "integer", nullable: false),
                    OffsetUnit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteReminder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoteReminder_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NoteReminder_NoteId",
                table: "NoteReminder",
                column: "NoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NoteReminder");

            migrationBuilder.DropColumn(
                name: "RecurrenceIntervalUnit",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "RecurrenceIntervalValue",
                table: "Notes");

            migrationBuilder.AddColumn<DateTime>(
                name: "Reminder1hSentAtUtc",
                table: "Notes",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
