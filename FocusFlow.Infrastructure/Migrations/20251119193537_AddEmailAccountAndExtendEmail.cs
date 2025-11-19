using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailAccountAndExtendEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EmailAccountId",
                table: "Emails",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalMessageId",
                table: "Emails",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "Emails",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "EmailAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    EmailAddress = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AccessToken = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: false),
                    AccessTokenExpiresUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConnectedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAccounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Emails_EmailAccountId",
                table: "Emails",
                column: "EmailAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_ExternalMessageId",
                table: "Emails",
                column: "ExternalMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_EmailAddress",
                table: "EmailAccounts",
                column: "EmailAddress");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_Provider",
                table: "EmailAccounts",
                column: "Provider");

            migrationBuilder.AddForeignKey(
                name: "FK_Emails_EmailAccounts_EmailAccountId",
                table: "Emails",
                column: "EmailAccountId",
                principalTable: "EmailAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Emails_EmailAccounts_EmailAccountId",
                table: "Emails");

            migrationBuilder.DropTable(
                name: "EmailAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Emails_EmailAccountId",
                table: "Emails");

            migrationBuilder.DropIndex(
                name: "IX_Emails_ExternalMessageId",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "EmailAccountId",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "ExternalMessageId",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "Emails");
        }
    }
}
