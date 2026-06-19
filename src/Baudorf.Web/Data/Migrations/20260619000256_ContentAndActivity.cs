using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baudorf.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ContentAndActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomeSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Overline = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Titel = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BildUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CtaText = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CtaUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Cta2Text = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Cta2Url = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Reihenfolge = table.Column<int>(type: "int", nullable: false),
                    IstSichtbar = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeSections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IpAdresse = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginEvents_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PropertyViews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IpAdresse = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyViews_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PropertyViews_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppClicks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PropertyId = table.Column<int>(type: "int", nullable: true),
                    Quelle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IpAdresse = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppClicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppClicks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WhatsAppClicks_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HomeSectionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HomeSectionId = table.Column<int>(type: "int", nullable: false),
                    Titel = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BildUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Reihenfolge = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeSectionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomeSectionItems_HomeSections_HomeSectionId",
                        column: x => x.HomeSectionId,
                        principalTable: "HomeSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomeSectionItems_HomeSectionId",
                table: "HomeSectionItems",
                column: "HomeSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_HomeSections_Key",
                table: "HomeSections",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoginEvents_UserId",
                table: "LoginEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyViews_CreatedAt",
                table: "PropertyViews",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyViews_PropertyId",
                table: "PropertyViews",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyViews_UserId",
                table: "PropertyViews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppClicks_PropertyId",
                table: "WhatsAppClicks",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppClicks_UserId",
                table: "WhatsAppClicks",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeSectionItems");

            migrationBuilder.DropTable(
                name: "LoginEvents");

            migrationBuilder.DropTable(
                name: "PropertyViews");

            migrationBuilder.DropTable(
                name: "WhatsAppClicks");

            migrationBuilder.DropTable(
                name: "HomeSections");
        }
    }
}
