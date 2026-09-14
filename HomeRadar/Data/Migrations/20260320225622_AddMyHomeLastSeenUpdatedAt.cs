using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeRadar.Migrations
{
    /// <inheritdoc />
    public partial class AddMyHomeLastSeenUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastSeenUpdatedAt",
                table: "MyHomeSavedFilters",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSeenUpdatedAt",
                table: "MyHomeSavedFilters");
        }
    }
}
