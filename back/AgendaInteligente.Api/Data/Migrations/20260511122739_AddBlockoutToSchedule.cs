using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaInteligente.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockoutToSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "service_id",
                table: "schedules",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "schedules",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "block_reason",
                table: "schedules",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_all_day",
                table: "schedules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_blocked",
                table: "schedules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_schedules_blockouts",
                table: "schedules",
                columns: new[] { "tenant_id", "professional_id", "is_blocked" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_schedules_blockouts",
                table: "schedules");

            migrationBuilder.DropColumn(
                name: "block_reason",
                table: "schedules");

            migrationBuilder.DropColumn(
                name: "is_all_day",
                table: "schedules");

            migrationBuilder.DropColumn(
                name: "is_blocked",
                table: "schedules");

            migrationBuilder.AlterColumn<Guid>(
                name: "service_id",
                table: "schedules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "schedules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
