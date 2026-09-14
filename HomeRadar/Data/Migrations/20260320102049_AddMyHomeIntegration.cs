using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeRadar.Migrations
{
    /// <inheritdoc />
    public partial class AddMyHomeIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MyHomeCities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MyHomeCities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MyHomeSavedFilters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SearchQueryString = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TelegramChatId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TelegramLinkCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LastSeenListingId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastCheckedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MyHomeSavedFilters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MyHomeSavedFilters_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MyHomeDistricts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MyHomeDistricts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MyHomeDistricts_MyHomeCities_CityId",
                        column: x => x.CityId,
                        principalTable: "MyHomeCities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MyHomeUrbans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DistrictId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MyHomeUrbans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MyHomeUrbans_MyHomeDistricts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "MyHomeDistricts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MyHomeDistricts_CityId",
                table: "MyHomeDistricts",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_MyHomeSavedFilters_IsActive_LastCheckedAt",
                table: "MyHomeSavedFilters",
                columns: new[] { "IsActive", "LastCheckedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MyHomeSavedFilters_UserId",
                table: "MyHomeSavedFilters",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MyHomeUrbans_DistrictId",
                table: "MyHomeUrbans",
                column: "DistrictId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MyHomeSavedFilters");

            migrationBuilder.DropTable(
                name: "MyHomeUrbans");

            migrationBuilder.DropTable(
                name: "MyHomeDistricts");

            migrationBuilder.DropTable(
                name: "MyHomeCities");
        }
    }
}
