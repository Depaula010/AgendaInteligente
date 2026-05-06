using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaInteligente.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGeminiConfigToTenantSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "gemini_api_key",
                table: "tenant_settings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gemini_model",
                table: "tenant_settings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "gemini-2.5-flash-lite");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gemini_api_key",
                table: "tenant_settings");

            migrationBuilder.DropColumn(
                name: "gemini_model",
                table: "tenant_settings");
        }
    }
}
