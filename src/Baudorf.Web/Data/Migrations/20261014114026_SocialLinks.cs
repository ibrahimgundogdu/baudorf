using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baudorf.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SocialLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SocialLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Plattform = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Bezeichnung = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Reihenfolge = table.Column<int>(type: "int", nullable: false),
                    IstAktiv = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialLinks", x => x.Id);
                });

            // Bestehende Profile aus den bisherigen festen Einstellungen übernehmen, damit
            // nach dem Update nichts aus dem Footer verschwindet. Leere Werte werden
            // übersprungen; fehlt das Schema, wird https:// ergänzt. Nur http(s) bzw.
            // schemalose Adressen werden übernommen (kein "javascript:" o. Ä.).
            migrationBuilder.Sql(@"
INSERT INTO SocialLinks (Plattform, Url, Bezeichnung, Reihenfolge, IstAktiv)
SELECT s.Plattform,
       CASE WHEN s.Raw LIKE '%://%' THEN s.Raw ELSE 'https://' + s.Raw END,
       NULL, s.Ord, 1
FROM (
    SELECT N'instagram' AS Plattform, 0 AS Ord, LTRIM(RTRIM(ISNULL([Value], N''))) AS Raw FROM SiteSettings WHERE [Key] = N'social.instagram'
    UNION ALL
    SELECT N'linkedin', 1, LTRIM(RTRIM(ISNULL([Value], N''))) FROM SiteSettings WHERE [Key] = N'social.linkedin'
    UNION ALL
    SELECT N'youtube', 2, LTRIM(RTRIM(ISNULL([Value], N''))) FROM SiteSettings WHERE [Key] = N'social.youtube'
) s
WHERE s.Raw <> N''
  AND LEN(s.Raw) <= 480
  AND (s.Raw LIKE N'http://%' OR s.Raw LIKE N'https://%' OR s.Raw NOT LIKE N'%:%');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SocialLinks");
        }
    }
}
