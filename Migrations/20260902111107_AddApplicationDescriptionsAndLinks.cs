using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationDescriptionsAndLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationDescription",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobApplicationId = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationDescription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationDescription_JobApplications_JobApplicationId",
                        column: x => x.JobApplicationId,
                        principalTable: "JobApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationLink",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobApplicationId = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationLink_JobApplications_JobApplicationId",
                        column: x => x.JobApplicationId,
                        principalTable: "JobApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDescription_JobApplicationId",
                table: "ApplicationDescription",
                column: "JobApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationLink_JobApplicationId",
                table: "ApplicationLink",
                column: "JobApplicationId");

            // Backfill existing single Description/Link values into the new
            // one-to-many tables before the old columns are dropped below.
            migrationBuilder.Sql(
                """
                INSERT INTO "ApplicationDescription" ("JobApplicationId", "Text")
                SELECT "Id", "Description" FROM "JobApplications"
                WHERE "Description" IS NOT NULL AND btrim("Description") <> '';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "ApplicationLink" ("JobApplicationId", "Url")
                SELECT "Id", "Link" FROM "JobApplications"
                WHERE "Link" IS NOT NULL AND btrim("Link") <> '';
                """);

            migrationBuilder.DropColumn(
                name: "Description",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "Link",
                table: "JobApplications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "JobApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Link",
                table: "JobApplications",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            // Best-effort restore: the old schema only ever held one of each,
            // so take the first row (lowest Id) per application.
            migrationBuilder.Sql(
                """
                UPDATE "JobApplications" j
                SET "Description" = d."Text"
                FROM (
                    SELECT DISTINCT ON ("JobApplicationId") "JobApplicationId", "Text"
                    FROM "ApplicationDescription"
                    ORDER BY "JobApplicationId", "Id"
                ) d
                WHERE d."JobApplicationId" = j."Id";
                """);

            migrationBuilder.Sql(
                """
                UPDATE "JobApplications" j
                SET "Link" = l."Url"
                FROM (
                    SELECT DISTINCT ON ("JobApplicationId") "JobApplicationId", "Url"
                    FROM "ApplicationLink"
                    ORDER BY "JobApplicationId", "Id"
                ) l
                WHERE l."JobApplicationId" = j."Id";
                """);

            migrationBuilder.DropTable(
                name: "ApplicationDescription");

            migrationBuilder.DropTable(
                name: "ApplicationLink");
        }
    }
}
