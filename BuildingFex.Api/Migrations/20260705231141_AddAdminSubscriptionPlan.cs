using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFex.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminSubscriptionPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "subscription_paid_until",
                table: "users",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subscription_plan_id",
                table: "users",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "free");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "subscription_paid_until",
                table: "users");

            migrationBuilder.DropColumn(
                name: "subscription_plan_id",
                table: "users");
        }
    }
}
