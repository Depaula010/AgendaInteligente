using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaInteligente.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReceptionistRoleAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "can_manage_services",
                table: "professionals",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "phone_number",
                table: "customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "can_manage_services",
                table: "professionals");

            migrationBuilder.AlterColumn<string>(
                name: "phone_number",
                table: "customers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
