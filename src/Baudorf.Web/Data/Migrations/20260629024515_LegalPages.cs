using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baudorf.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class LegalPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegalPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Titel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Overline = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    BodyHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalPages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegalPages_Slug",
                table: "LegalPages",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegalPages");
        }
    }
}
