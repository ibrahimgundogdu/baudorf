using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baudorf.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class WiderrufAntrag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WiderrufAntraege",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Vorname = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Nachname = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Strasse = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PlzOrt = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Vertragsidentifikation = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    DatumBeschreibung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Bestaetigt = table.Column<bool>(type: "bit", nullable: false),
                    IpAdresse = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Erledigt = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WiderrufAntraege", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WiderrufAntraege");
        }
    }
}
