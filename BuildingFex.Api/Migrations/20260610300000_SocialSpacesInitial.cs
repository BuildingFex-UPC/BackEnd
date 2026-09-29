using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace BuildingFex.Api.Migrations
{
    /// <inheritdoc />
    public partial class SocialSpacesInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "social_spaces",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    capacity = table.Column<int>(type: "int", nullable: true),
                    image_url = table.Column<string>(type: "longtext", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_social_spaces", x => x.id);
                    table.ForeignKey(
                        name: "f_k_social_spaces_users_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "reservations",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "int", nullable: false),
                    space_external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    resident_external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    resident_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    resident_code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    date = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    start_time = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    end_time = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    guests_json = table.Column<string>(type: "longtext", nullable: false),
                    guest_invite_token = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_reservations", x => x.id);
                    table.ForeignKey(
                        name: "f_k_reservations_users_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "i_x_social_spaces_external_id",
                table: "social_spaces",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_social_spaces_owner_admin_id",
                table: "social_spaces",
                column: "owner_admin_id");

            migrationBuilder.CreateIndex(
                name: "i_x_reservations_external_id",
                table: "reservations",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_reservations_guest_invite_token",
                table: "reservations",
                column: "guest_invite_token");

            migrationBuilder.CreateIndex(
                name: "i_x_reservations_owner_admin_id",
                table: "reservations",
                column: "owner_admin_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "reservations");
            migrationBuilder.DropTable(name: "social_spaces");
        }
    }
}
