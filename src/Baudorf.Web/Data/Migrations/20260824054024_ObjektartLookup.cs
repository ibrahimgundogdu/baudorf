using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baudorf.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ObjektartLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArtKey",
                table: "Properties",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            // Bestehende Objekte: ArtKey aus der alten Enum-Spalte "Art" befüllen (kein Datenverlust).
            migrationBuilder.Sql(@"
                UPDATE [Properties] SET [ArtKey] = CASE [Art]
                    WHEN 0 THEN 'offmarket'
                    WHEN 1 THEN 'kapitalanlage'
                    WHEN 2 THEN 'investment'
                    WHEN 3 THEN 'gewerbe'
                    WHEN 4 THEN 'wohnimmobilie'
                    WHEN 5 THEN 'grundstueck'
                    WHEN 6 THEN 'projektentwicklung'
                    WHEN 7 THEN 'auslandsimmobilie'
                    ELSE 'offmarket' END
                WHERE [ArtKey] = '' OR [ArtKey] IS NULL;");

            migrationBuilder.CreateTable(
                name: "LookupOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kategorie = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Wert = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Reihenfolge = table.Column<int>(type: "int", nullable: false),
                    IstAktiv = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupOptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LookupOptions_Kategorie_Wert",
                table: "LookupOptions",
                columns: new[] { "Kategorie", "Wert" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LookupOptions");

            migrationBuilder.DropColumn(
                name: "ArtKey",
                table: "Properties");
        }
    }
}
