using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SkillSwap.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningPathEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assessment_blueprints",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    path_node_id = table.Column<int>(type: "integer", nullable: false),
                    skill_tag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    questions = table.Column<string>(type: "jsonb", nullable: false),
                    generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assessment_blueprints", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "learning_paths",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    career_goal = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_paths", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "path_nodes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    skill_tag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    node_order = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    prerequisites = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    linked_certificate_id = table.Column<int>(type: "integer", nullable: true),
                    assessment_blueprint_id = table.Column<int>(type: "integer", nullable: true),
                    learning_path_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_path_nodes", x => x.id);
                    table.ForeignKey(
                        name: "FK_path_nodes_learning_paths_learning_path_id",
                        column: x => x.learning_path_id,
                        principalTable: "learning_paths",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assessment_blueprints_path_node_id",
                table: "assessment_blueprints",
                column: "path_node_id");

            migrationBuilder.CreateIndex(
                name: "ux_learning_paths_one_active_per_student",
                table: "learning_paths",
                column: "student_id",
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_path_nodes_learning_path_id_node_order",
                table: "path_nodes",
                columns: new[] { "learning_path_id", "node_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_path_nodes_learning_path_id_skill_tag",
                table: "path_nodes",
                columns: new[] { "learning_path_id", "skill_tag" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assessment_blueprints");

            migrationBuilder.DropTable(
                name: "path_nodes");

            migrationBuilder.DropTable(
                name: "learning_paths");
        }
    }
}
