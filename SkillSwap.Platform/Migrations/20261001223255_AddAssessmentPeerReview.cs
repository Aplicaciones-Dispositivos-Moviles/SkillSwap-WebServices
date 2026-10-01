using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SkillSwap.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentPeerReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assessment_attempts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    blueprint_id = table.Column<int>(type: "integer", nullable: false),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    selected_answers = table.Column<string>(type: "jsonb", nullable: false),
                    score = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    passed = table.Column<bool>(type: "boolean", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assessment_attempts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "verification_cases",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    attempt_id = table.Column<int>(type: "integer", nullable: false),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    verifier_user_id = table.Column<int>(type: "integer", nullable: true),
                    path_node_id = table.Column<int>(type: "integer", nullable: false),
                    skill_tag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    rubric_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    evidence_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verification_cases", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "verifier_profiles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    verifier_user_id = table.Column<int>(type: "integer", nullable: false),
                    skill_tags = table.Column<string>(type: "jsonb", nullable: false),
                    available = table.Column<bool>(type: "boolean", nullable: false),
                    verified = table.Column<bool>(type: "boolean", nullable: false),
                    rating = table.Column<double>(type: "double precision", nullable: false),
                    review_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verifier_profiles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assessment_attempts_student_id",
                table: "assessment_attempts",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ux_assessment_attempts_blueprint",
                table: "assessment_attempts",
                column: "blueprint_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_verification_cases_skill_tag_status",
                table: "verification_cases",
                columns: new[] { "skill_tag", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_verification_cases_verifier_user_id",
                table: "verification_cases",
                column: "verifier_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_verification_cases_attempt",
                table: "verification_cases",
                column: "attempt_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_verification_cases_one_open_per_student_node",
                table: "verification_cases",
                columns: new[] { "student_id", "path_node_id" },
                unique: true,
                filter: "status <> 'Resolved'");

            migrationBuilder.CreateIndex(
                name: "ux_verifier_profiles_user",
                table: "verifier_profiles",
                column: "verifier_user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assessment_attempts");

            migrationBuilder.DropTable(
                name: "verification_cases");

            migrationBuilder.DropTable(
                name: "verifier_profiles");
        }
    }
}
