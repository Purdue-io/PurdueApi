using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class MultipleClassesPerSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new many-to-many table
            migrationBuilder.CreateTable(
                name: "ClassSection",
                columns: table => new
                {
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassSection", x => new { x.ClassId, x.SectionId });
                    table.ForeignKey(
                        name: "FK_ClassSection_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassSection_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassSection_ClassId",
                table: "ClassSection",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSection_SectionId",
                table: "ClassSection",
                column: "SectionId");

            // Migrate existing data
            migrationBuilder.Sql("INSERT INTO \"ClassSection\" (\"ClassId\", \"SectionId\") " +
                "SELECT \"ClassId\", \"Id\" FROM \"Sections\";");

            // Drop old relationship
            migrationBuilder.DropForeignKey(
                name: "FK_Sections_Classes_ClassId",
                table: "Sections");

            migrationBuilder.DropIndex(
                name: "IX_Sections_ClassId",
                table: "Sections");

            migrationBuilder.DropColumn(name: "ClassId", table: "Sections");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassSection");

            migrationBuilder.CreateIndex(
                name: "IX_Sections_ClassId",
                table: "Sections",
                column: "ClassId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sections_Classes_ClassId",
                table: "Sections",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
