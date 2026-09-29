using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace BuildingFex.Api.Migrations
{
    /// <inheritdoc />
    public partial class InformationInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "announcements",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    body = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false),
                    priority = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    duration = table.Column<int>(type: "int", nullable: false),
                    author_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_announcements", x => x.id);
                    table.ForeignKey(
                        name: "f_k_announcements_users_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "i_x_announcements_external_id",
                table: "announcements",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_announcements_owner_admin_id",
                table: "announcements",
                column: "owner_admin_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "announcements");
        }
    }
}
