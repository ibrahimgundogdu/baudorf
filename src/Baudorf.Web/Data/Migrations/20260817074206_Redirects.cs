using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baudorf.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Redirects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotFoundHits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Pfad = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Anzahl = table.Column<int>(type: "int", nullable: false),
                    LetzterReferrer = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    Erledigt = table.Column<bool>(type: "bit", nullable: false),
                    Zuerst = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Zuletzt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotFoundHits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Redirects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VonPfad = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    NachPfad = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    Code = table.Column<int>(type: "int", nullable: false),
                    IstAktiv = table.Column<bool>(type: "bit", nullable: false),
                    Treffer = table.Column<int>(type: "int", nullable: false),
                    LetzterTreffer = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Notiz = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Redirects", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotFoundHits_Pfad",
                table: "NotFoundHits",
                column: "Pfad",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Redirects_VonPfad",
                table: "Redirects",
                column: "VonPfad",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotFoundHits");

            migrationBuilder.DropTable(
                name: "Redirects");
        }
    }
}
