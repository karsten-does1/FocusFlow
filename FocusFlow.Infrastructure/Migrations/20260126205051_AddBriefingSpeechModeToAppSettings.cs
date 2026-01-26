using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBriefingSpeechModeToAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BriefingSpeechMode",
                table: "AppSettings",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Expanded");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BriefingSpeechMode",
                table: "AppSettings");
        }
    }
}
