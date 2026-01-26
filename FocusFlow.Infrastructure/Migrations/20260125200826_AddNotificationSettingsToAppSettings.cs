using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSettingsToAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BriefingNotificationsEnabled",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "BriefingTimeLocal",
                table: "AppSettings",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "09:00");

            migrationBuilder.AddColumn<int>(
                name: "NotificationTickSeconds",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AddColumn<bool>(
                name: "NotificationsEnabled",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "ReminderUpcomingWindowMinutes",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BriefingNotificationsEnabled",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "BriefingTimeLocal",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "NotificationTickSeconds",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "NotificationsEnabled",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "ReminderUpcomingWindowMinutes",
                table: "AppSettings");
        }
    }
}
