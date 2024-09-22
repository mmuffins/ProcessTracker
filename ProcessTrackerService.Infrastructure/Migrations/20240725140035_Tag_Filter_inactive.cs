using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessTrackerService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Tag_Filter_inactive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Inactive",
                table: "Tags",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Inactive",
                table: "Filters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Inactive",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "Inactive",
                table: "Filters");
        }
    }
}
