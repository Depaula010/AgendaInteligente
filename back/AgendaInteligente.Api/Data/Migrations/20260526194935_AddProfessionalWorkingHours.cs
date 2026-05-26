using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaInteligente.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProfessionalWorkingHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "working_hours_json",
                table: "professionals",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "working_hours_json",
                table: "professionals");
        }
    }
}
