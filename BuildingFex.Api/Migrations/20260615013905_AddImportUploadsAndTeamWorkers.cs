using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace BuildingFex.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddImportUploadsAndTeamWorkers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "import_uploads",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    external_id = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    owner_admin_id = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    file_name = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    mime_type = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    data_url = table.Column<string>(type: "LONGTEXT", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_import_uploads", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "team_workers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    external_id = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    owner_admin_id = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    phone = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    dni = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    salary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    photo_url = table.Column<string>(type: "LONGTEXT", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_team_workers", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "i_x_import_uploads_external_id",
                table: "import_uploads",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_team_workers_external_id",
                table: "team_workers",
                column: "external_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "import_uploads");

            migrationBuilder.DropTable(
                name: "team_workers");
        }
    }
}
