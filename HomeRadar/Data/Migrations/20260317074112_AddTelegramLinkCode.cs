using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeRadar.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramLinkCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TelegramLinkCode",
                table: "SavedFilters",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TelegramLinkCode",
                table: "SavedFilters");
        }
    }
}
