using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baudorf.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class PropertyVideoLeadAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Gewerbeflaeche",
                table: "Properties",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "Properties",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Firma",
                table: "Leads",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ort",
                table: "Leads",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Plz",
                table: "Leads",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Strasse",
                table: "Leads",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gewerbeflaeche",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "Firma",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Ort",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Plz",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Strasse",
                table: "Leads");
        }
    }
}
