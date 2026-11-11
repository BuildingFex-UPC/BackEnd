using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BuildingFex.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCostToSocialSpace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "import_uploads",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    owner_admin_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    data_url = table.Column<string>(type: "text", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_import_uploads", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_checkout_orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    preference_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "integer", nullable: false),
                    resident_external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    receipt_ids_json = table.Column<string>(type: "text", nullable: false),
                    fee_ids_json = table.Column<string>(type: "text", nullable: false),
                    mp_payment_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_maintenance_checkout_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "support_chats",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    owner_admin_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    resident_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    resident_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    topic = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    messages_json = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_support_chats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "team_workers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    owner_admin_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    dni = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    salary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    photo_url = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_team_workers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    dni = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ruc = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    floor = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    admission_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    avatar = table.Column<string>(type: "text", nullable: true),
                    owner_admin_id = table.Column<int>(type: "integer", nullable: true),
                    subscription_plan_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "free"),
                    subscription_paid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_users", x => x.id);
                    table.ForeignKey(
                        name: "f_k_users_users_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admin_management_expenses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    purchase_date = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    invoice_photo_url = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_admin_management_expenses", x => x.id);
                    table.ForeignKey(
                        name: "f_k_admin_management_expenses__user_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "announcements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    duration = table.Column<int>(type: "integer", nullable: false),
                    author_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "fees",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "integer", nullable: false),
                    resident_external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    month = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    due_date = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_fees", x => x.id);
                    table.ForeignKey(
                        name: "f_k_fees__user_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "finance_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "integer", nullable: false),
                    base_monthly_expense = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    late_fee_rate = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_finance_settings", x => x.id);
                    table.ForeignKey(
                        name: "f_k_finance_settings__user_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fixed_payout_recipients",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dni = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    salary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    interval_days = table.Column<int>(type: "integer", nullable: false),
                    next_payment_date = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    photo_url = table.Column<string>(type: "text", nullable: false),
                    payment_history_json = table.Column<string>(type: "text", nullable: false),
                    created_at_iso = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_fixed_payout_recipients", x => x.id);
                    table.ForeignKey(
                        name: "f_k_fixed_payout_recipients__user_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "incidents",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "integer", nullable: false),
                    resident_external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    resident_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    provider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "kpi_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "integer", nullable: false),
                    total_residents = table.Column<int>(type: "integer", nullable: false),
                    occupied_units = table.Column<int>(type: "integer", nullable: false),
                    empty_units = table.Column<int>(type: "integer", nullable: false),
                    total_debt = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_kpi_records", x => x.id);
                    table.ForeignKey(
                        name: "f_k_kpi_records__user_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "integer", nullable: false),
                    resident_external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    fee_external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    fee_month = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_at = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    method = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    reference = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_payments", x => x.id);
                    table.ForeignKey(
                        name: "f_k_payments__user_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receipts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "integer", nullable: false),
                    resident_external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    issue_date = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    due_date = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    late_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    extra_charges = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    concept = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_receipts", x => x.id);
                    table.ForeignKey(
                        name: "f_k_receipts__user_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reservations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "integer", nullable: false),
                    space_external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    resident_external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    resident_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    resident_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    date = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    start_time = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    end_time = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    guests_json = table.Column<string>(type: "text", nullable: false),
                    guest_invite_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "shared_utility_services",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    month = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    resident_count = table.Column<int>(type: "integer", nullable: true),
                    per_resident_share = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_shared_utility_services", x => x.id);
                    table.ForeignKey(
                        name: "f_k_shared_utility_services__user_owner_admin_id",
                        column: x => x.owner_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "social_spaces",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner_admin_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: false),
                    cost = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                });

            migrationBuilder.CreateIndex(
                name: "i_x_admin_management_expenses_external_id",
                table: "admin_management_expenses",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_admin_management_expenses_owner_admin_id",
                table: "admin_management_expenses",
                column: "owner_admin_id");

            migrationBuilder.CreateIndex(
                name: "i_x_announcements_external_id",
                table: "announcements",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_announcements_owner_admin_id",
                table: "announcements",
                column: "owner_admin_id");

            migrationBuilder.CreateIndex(
                name: "i_x_fees_external_id",
                table: "fees",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_fees_owner_admin_id",
                table: "fees",
                column: "owner_admin_id");

            migrationBuilder.CreateIndex(
                name: "i_x_finance_settings_external_id",
                table: "finance_settings",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_finance_settings_owner_admin_id",
                table: "finance_settings",
                column: "owner_admin_id");

            migrationBuilder.CreateIndex(
                name: "i_x_fixed_payout_recipients_external_id",
                table: "fixed_payout_recipients",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_fixed_payout_recipients_owner_admin_id",
                table: "fixed_payout_recipients",
                column: "owner_admin_id");

            migrationBuilder.CreateIndex(
                name: "i_x_import_uploads_external_id",
                table: "import_uploads",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_incidents_external_id",
                table: "incidents",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_incidents_owner_admin_id",
                table: "incidents",
                column: "owner_admin_id");

            migrationBuilder.CreateIndex(
                name: "i_x_kpi_records_external_id",
                table: "kpi_records",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_kpi_records_owner_admin_id",
                table: "kpi_records",
                column: "owner_admin_id");

            migrationBuilder.CreateIndex(
                name: "i_x_maintenance_checkout_orders_external_id",
                table: "maintenance_checkout_orders",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_maintenance_checkout_orders_preference_id",
                table: "maintenance_checkout_orders",
                column: "preference_id");

            migrationBuilder.CreateIndex(
                name: "i_x_payments_external_id",
                table: "payments",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_payments_owner_admin_id",
                table: "payments",
                column: "owner_admin_id");

            migrationBuilder.CreateIndex(
                name: "i_x_receipts_external_id",
                table: "receipts",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_receipts_owner_admin_id",
                table: "receipts",
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

            migrationBuilder.CreateIndex(
                name: "i_x_shared_utility_services_external_id",
                table: "shared_utility_services",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_shared_utility_services_owner_admin_id",
                table: "shared_utility_services",
                column: "owner_admin_id");

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
                name: "i_x_support_chats_external_id",
                table: "support_chats",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_team_workers_external_id",
                table: "team_workers",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_users_external_id",
                table: "users",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_users_owner_admin_id",
                table: "users",
                column: "owner_admin_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_management_expenses");

            migrationBuilder.DropTable(
                name: "announcements");

            migrationBuilder.DropTable(
                name: "fees");

            migrationBuilder.DropTable(
                name: "finance_settings");

            migrationBuilder.DropTable(
                name: "fixed_payout_recipients");

            migrationBuilder.DropTable(
                name: "import_uploads");

            migrationBuilder.DropTable(
                name: "incidents");

            migrationBuilder.DropTable(
                name: "kpi_records");

            migrationBuilder.DropTable(
                name: "maintenance_checkout_orders");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "receipts");

            migrationBuilder.DropTable(
                name: "reservations");

            migrationBuilder.DropTable(
                name: "shared_utility_services");

            migrationBuilder.DropTable(
                name: "social_spaces");

            migrationBuilder.DropTable(
                name: "support_chats");

            migrationBuilder.DropTable(
                name: "team_workers");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
