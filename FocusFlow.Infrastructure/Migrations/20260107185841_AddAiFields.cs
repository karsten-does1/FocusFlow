using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Emails",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SuggestedAction",
                table: "Emails",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "SuggestedAction",
                table: "Emails");
        }
    }
}
