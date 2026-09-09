using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebApp.Migrations
{
    /// <inheritdoc />
    public partial class ReworkTrainingIntoWorkoutsAndExerciseTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reordered from the scaffolded output: WorkoutLogs is dropped at the very
            // end of this method instead of the top, so its data is still there to
            // backfill Workouts/WorkoutExercises/WorkoutSets from further down.

            // FreeWeight is the correct default for every existing exercise regardless
            // of history: the old flow always had you type in whatever weight you were
            // lifting, which is exactly FreeWeight's semantics.
            migrationBuilder.AddColumn<string>(
                name: "SessionType",
                table: "Exercises",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Push");

            migrationBuilder.AddColumn<string>(
                name: "WeightType",
                table: "Exercises",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "FreeWeight");

            migrationBuilder.CreateTable(
                name: "ExerciseMachineAddOn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExerciseId = table.Column<int>(type: "integer", nullable: false),
                    AddOnKg = table.Column<decimal>(type: "numeric(6,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseMachineAddOn", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseMachineAddOn_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseMachineWeight",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExerciseId = table.Column<int>(type: "integer", nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(6,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseMachineWeight", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseMachineWeight_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Workouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    SessionType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workouts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutExercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkoutId = table.Column<int>(type: "integer", nullable: false),
                    ExerciseId = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutExercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutExercises_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkoutExercises_Workouts_WorkoutId",
                        column: x => x.WorkoutId,
                        principalTable: "Workouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkoutExerciseId = table.Column<int>(type: "integer", nullable: false),
                    SetNumber = table.Column<int>(type: "integer", nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    BaseWeightKg = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    AddOnKg = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    Reps = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutSets_WorkoutExercises_WorkoutExerciseId",
                        column: x => x.WorkoutExerciseId,
                        principalTable: "WorkoutExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseMachineAddOn_ExerciseId",
                table: "ExerciseMachineAddOn",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseMachineWeight_ExerciseId",
                table: "ExerciseMachineWeight",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercises_ExerciseId",
                table: "WorkoutExercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercises_WorkoutId",
                table: "WorkoutExercises",
                column: "WorkoutId");

            migrationBuilder.CreateIndex(
                name: "IX_Workouts_UserId_Date",
                table: "Workouts",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSets_WorkoutExerciseId",
                table: "WorkoutSets",
                column: "WorkoutExerciseId");

            // --- Data backfill, while WorkoutLogs still exists ---

            // Exercise.SessionType: infer from whichever SessionType was logged most
            // often for it historically; an exercise with no logs at all keeps the
            // "Push" column default set above (there's no history to infer from).
            migrationBuilder.Sql("""
                UPDATE "Exercises" e
                SET "SessionType" = ranked."SessionType"
                FROM (
                    SELECT "ExerciseId", "SessionType",
                           ROW_NUMBER() OVER (PARTITION BY "ExerciseId" ORDER BY COUNT(*) DESC) AS rn
                    FROM "WorkoutLogs"
                    GROUP BY "ExerciseId", "SessionType"
                ) AS ranked
                WHERE ranked.rn = 1 AND e."Id" = ranked."ExerciseId";
                """);

            // One Workout + one WorkoutExercise per historical WorkoutLog, reusing the
            // WorkoutLog's own Id as the Id of both (a stable 1:1:1 correlation key,
            // safe since both target tables are brand new and otherwise empty).
            migrationBuilder.Sql("""
                INSERT INTO "Workouts" ("Id", "UserId", "Date", "Time", "SessionType")
                SELECT "Id", "UserId", "Date", NULL, "SessionType"
                FROM "WorkoutLogs";
                """);
            migrationBuilder.Sql("""
                SELECT setval(pg_get_serial_sequence('"Workouts"', 'Id'), COALESCE((SELECT MAX("Id") FROM "Workouts"), 1));
                """);

            migrationBuilder.Sql("""
                INSERT INTO "WorkoutExercises" ("Id", "WorkoutId", "ExerciseId", "SortOrder")
                SELECT "Id", "Id", "ExerciseId", 0
                FROM "WorkoutLogs";
                """);
            migrationBuilder.Sql("""
                SELECT setval(pg_get_serial_sequence('"WorkoutExercises"', 'Id'), COALESCE((SELECT MAX("Id") FROM "WorkoutExercises"), 1));
                """);

            // Fan each WorkoutLog out into N WorkoutSet rows (N = its old Sets count),
            // all sharing the same historical WeightKg/Reps -- the old model never
            // distinguished per-set values, so this is the most faithful preservation.
            migrationBuilder.Sql("""
                INSERT INTO "WorkoutSets" ("WorkoutExerciseId", "SetNumber", "WeightKg", "Reps")
                SELECT wl."Id", gs.n, wl."WeightKg", wl."Reps"
                FROM "WorkoutLogs" wl
                CROSS JOIN LATERAL generate_series(1, wl."Sets") AS gs(n);
                """);

            migrationBuilder.DropTable(
                name: "WorkoutLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseMachineAddOn");

            migrationBuilder.DropTable(
                name: "ExerciseMachineWeight");

            migrationBuilder.DropTable(
                name: "WorkoutSets");

            migrationBuilder.DropTable(
                name: "WorkoutExercises");

            migrationBuilder.DropTable(
                name: "Workouts");

            migrationBuilder.DropColumn(
                name: "SessionType",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "WeightType",
                table: "Exercises");

            migrationBuilder.CreateTable(
                name: "WorkoutLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExerciseId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Reps = table.Column<int>(type: "integer", nullable: false),
                    SessionType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Sets = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(6,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkoutLogs_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutLogs_ExerciseId",
                table: "WorkoutLogs",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutLogs_UserId_ExerciseId",
                table: "WorkoutLogs",
                columns: new[] { "UserId", "ExerciseId" });
        }
    }
}
