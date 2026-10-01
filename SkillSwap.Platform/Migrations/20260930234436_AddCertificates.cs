using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SkillSwap.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "certificates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    owner_id = table.Column<int>(type: "integer", nullable: false),
                    holder_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    institution_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    course_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: true),
                    duration_hours = table.Column<int>(type: "integer", nullable: true),
                    certificate_number = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    verification_code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    verification_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    qr_payload = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    ocr_text = table.Column<string>(type: "text", nullable: false),
                    file_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    storage_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    verification_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    risk_score = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_certificates_certificate_number",
                table: "certificates",
                column: "certificate_number");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_file_hash",
                table: "certificates",
                column: "file_hash");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_owner_id_file_hash",
                table: "certificates",
                columns: new[] { "owner_id", "file_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_certificates_verification_code",
                table: "certificates",
                column: "verification_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "certificates");
        }
    }
}
