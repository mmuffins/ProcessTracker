using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessTrackerService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    SettingName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.SettingName);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Inactive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Filters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FilterType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    FieldType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    FieldValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false),
                    Inactive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Filters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Filters_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TagSessions",
                columns: table => new
                {
                    SessionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagSessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_TagSessions_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TagSessionSummary",
                columns: table => new
                {
                    SummaryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Day = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false),
                    Seconds = table.Column<double>(type: "REAL", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagSessionSummary", x => x.SummaryId);
                    table.ForeignKey(
                        name: "FK_TagSessionSummary_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Filters_TagId",
                table: "Filters",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_TagSessions_TagId",
                table: "TagSessions",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_TagSessionSummary_TagId",
                table: "TagSessionSummary",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Filters");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "TagSessions");

            migrationBuilder.DropTable(
                name: "TagSessionSummary");

            migrationBuilder.DropTable(
                name: "Tags");
        }
    }
}
