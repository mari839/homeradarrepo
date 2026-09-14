using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeRadar.Migrations
{
    /// <inheritdoc />
    public partial class AddMyHomePerPage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PerPage",
                table: "MyHomeSavedFilters",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PerPage",
                table: "MyHomeSavedFilters");
        }
    }
}
