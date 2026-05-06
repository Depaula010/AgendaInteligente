using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaInteligente.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleCalendarToProfessional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "google_calendar_email",
                table: "professionals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "google_calendar_refresh_token",
                table: "professionals",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "google_calendar_email",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "google_calendar_refresh_token",
                table: "professionals");
        }
    }
}
