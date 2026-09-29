using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace BuildingFex.Api.Migrations
{
    /// <inheritdoc />
    public partial class IncidentsInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "incidents",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "int", nullable: false),
                    resident_external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    resident_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    provider = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    reported_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_incidents", x => x.id);
                    table.ForeignKey(
                        name: "f_k_incidents_users_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "i_x_incidents_external_id",
                table: "incidents",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_incidents_owner_admin_id",
                table: "incidents",
                column: "owner_admin_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "incidents");
        }
    }
}
