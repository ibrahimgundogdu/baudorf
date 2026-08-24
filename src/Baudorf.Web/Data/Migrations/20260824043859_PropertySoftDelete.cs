using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baudorf.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class PropertySoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "GeloeschtAm",
                table: "Properties",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IstGeloescht",
                table: "Properties",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeloeschtAm",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "IstGeloescht",
                table: "Properties");
        }
    }
}
