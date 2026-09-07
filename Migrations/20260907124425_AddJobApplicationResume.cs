using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddJobApplicationResume : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResumeFileName",
                table: "JobApplications",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ResumeFileSizeBytes",
                table: "JobApplications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumeStoredFileName",
                table: "JobApplications",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResumeUploadedAtUtc",
                table: "JobApplications",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResumeFileName",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "ResumeFileSizeBytes",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "ResumeStoredFileName",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "ResumeUploadedAtUtc",
                table: "JobApplications");
        }
    }
}
