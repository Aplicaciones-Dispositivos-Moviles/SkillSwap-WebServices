using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SkillSwap.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddReputation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "student_employability_scores",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    verified_skills_count = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_employability_scores", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "verifier_reliabilities",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    verifier_user_id = table.Column<int>(type: "integer", nullable: false),
                    resolved_cases_count = table.Column<int>(type: "integer", nullable: false),
                    overturned_decisions_count = table.Column<int>(type: "integer", nullable: false),
                    sanctions_count = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verifier_reliabilities", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_student_employability_scores_student",
                table: "student_employability_scores",
                column: "student_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_verifier_reliabilities_user",
                table: "verifier_reliabilities",
                column: "verifier_user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "student_employability_scores");

            migrationBuilder.DropTable(
                name: "verifier_reliabilities");
        }
    }
}
