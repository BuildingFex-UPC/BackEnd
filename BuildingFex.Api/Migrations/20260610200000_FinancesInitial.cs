using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace BuildingFex.Api.Migrations
{
    /// <inheritdoc />
    public partial class FinancesInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fees",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "int", nullable: false),
                    resident_external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    month = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    due_date = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_fees", x => x.id);
                    table.ForeignKey(
                        name: "f_k_fees_users_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "int", nullable: false),
                    resident_external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    fee_external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    fee_month = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_at = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    method = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true),
                    reference = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_payments", x => x.id);
                    table.ForeignKey(
                        name: "f_k_payments_users_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "receipts",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "int", nullable: false),
                    resident_external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    issue_date = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    due_date = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    late_fee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    extra_charges = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    concept = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_receipts", x => x.id);
                    table.ForeignKey(
                        name: "f_k_receipts_users_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "finance_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "int", nullable: false),
                    base_monthly_expense = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    late_fee_rate = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_finance_settings", x => x.id);
                    table.ForeignKey(
                        name: "f_k_finance_settings_users_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "kpi_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "int", nullable: false),
                    total_residents = table.Column<int>(type: "int", nullable: false),
                    occupied_units = table.Column<int>(type: "int", nullable: false),
                    empty_units = table.Column<int>(type: "int", nullable: false),
                    total_debt = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_kpi_records", x => x.id);
                    table.ForeignKey(
                        name: "f_k_kpi_records_users_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "admin_management_expenses",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    purchase_date = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    invoice_photo_url = table.Column<string>(type: "longtext", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_admin_management_expenses", x => x.id);
                    table.ForeignKey(
                        name: "f_k_admin_management_expenses_users_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "shared_utility_services",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    month = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true),
                    resident_count = table.Column<int>(type: "int", nullable: true),
                    per_resident_share = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_shared_utility_services", x => x.id);
                    table.ForeignKey(
                        name: "f_k_shared_utility_services_users_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "fixed_payout_recipients",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    external_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    dni = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    phone = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    salary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    interval_days = table.Column<int>(type: "int", nullable: false),
                    next_payment_date = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    photo_url = table.Column<string>(type: "longtext", nullable: false),
                    payment_history_json = table.Column<string>(type: "longtext", nullable: false),
                    created_at_iso = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_fixed_payout_recipients", x => x.id);
                    table.ForeignKey(
                        name: "f_k_fixed_payout_recipients_users_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(name: "i_x_fees_external_id", table: "fees", column: "external_id", unique: true);
            migrationBuilder.CreateIndex(name: "i_x_fees_owner_admin_id", table: "fees", column: "owner_admin_id");
            migrationBuilder.CreateIndex(name: "i_x_payments_external_id", table: "payments", column: "external_id", unique: true);
            migrationBuilder.CreateIndex(name: "i_x_payments_owner_admin_id", table: "payments", column: "owner_admin_id");
            migrationBuilder.CreateIndex(name: "i_x_receipts_external_id", table: "receipts", column: "external_id", unique: true);
            migrationBuilder.CreateIndex(name: "i_x_receipts_owner_admin_id", table: "receipts", column: "owner_admin_id");
            migrationBuilder.CreateIndex(name: "i_x_finance_settings_external_id", table: "finance_settings", column: "external_id", unique: true);
            migrationBuilder.CreateIndex(name: "i_x_finance_settings_owner_admin_id", table: "finance_settings", column: "owner_admin_id");
            migrationBuilder.CreateIndex(name: "i_x_kpi_records_external_id", table: "kpi_records", column: "external_id", unique: true);
            migrationBuilder.CreateIndex(name: "i_x_kpi_records_owner_admin_id", table: "kpi_records", column: "owner_admin_id");
            migrationBuilder.CreateIndex(name: "i_x_admin_management_expenses_external_id", table: "admin_management_expenses", column: "external_id", unique: true);
            migrationBuilder.CreateIndex(name: "i_x_admin_management_expenses_owner_admin_id", table: "admin_management_expenses", column: "owner_admin_id");
            migrationBuilder.CreateIndex(name: "i_x_shared_utility_services_external_id", table: "shared_utility_services", column: "external_id", unique: true);
            migrationBuilder.CreateIndex(name: "i_x_shared_utility_services_owner_admin_id", table: "shared_utility_services", column: "owner_admin_id");
            migrationBuilder.CreateIndex(name: "i_x_fixed_payout_recipients_external_id", table: "fixed_payout_recipients", column: "external_id", unique: true);
            migrationBuilder.CreateIndex(name: "i_x_fixed_payout_recipients_owner_admin_id", table: "fixed_payout_recipients", column: "owner_admin_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "fees");
            migrationBuilder.DropTable(name: "payments");
            migrationBuilder.DropTable(name: "receipts");
            migrationBuilder.DropTable(name: "finance_settings");
            migrationBuilder.DropTable(name: "kpi_records");
            migrationBuilder.DropTable(name: "admin_management_expenses");
            migrationBuilder.DropTable(name: "shared_utility_services");
            migrationBuilder.DropTable(name: "fixed_payout_recipients");
        }
    }
}
