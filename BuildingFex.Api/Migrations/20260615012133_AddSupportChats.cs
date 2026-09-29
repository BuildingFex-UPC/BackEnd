using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace BuildingFex.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportChats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "admission_date",
                table: "users",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "support_chats",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    external_id = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    owner_admin_id = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    resident_id = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    resident_name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    topic = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    messages_json = table.Column<string>(type: "LONGTEXT", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_support_chats", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "i_x_support_chats_external_id",
                table: "support_chats",
                column: "external_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "support_chats");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "admission_date",
                table: "users",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);
        }
    }
}
