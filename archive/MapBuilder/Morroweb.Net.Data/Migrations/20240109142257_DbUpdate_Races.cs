#nullable disable
using Microsoft.EntityFrameworkCore.Migrations;
namespace Morroweb.Net.Migrations
{
    /// <inheritdoc />
    public partial class DbUpdate_Races : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Races",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Flags = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Races", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaceAttributes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RaceId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AttributeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Male = table.Column<int>(type: "int", nullable: false),
                    Female = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaceAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaceAttributes_Attributes_AttributeId",
                        column: x => x.AttributeId,
                        principalTable: "Attributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RaceAttributes_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RaceSkillBonuses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RaceId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SkillId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Bonus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaceSkillBonuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaceSkillBonuses_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RaceSkillBonuses_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RaceSpells",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RaceId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SpellId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaceSpells", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaceSpells_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RaceSpells_Spells_SpellId",
                        column: x => x.SpellId,
                        principalTable: "Spells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RaceStats",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RaceId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Male = table.Column<float>(type: "real", nullable: false),
                    Female = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaceStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaceStats_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RaceAttributes_AttributeId",
                table: "RaceAttributes",
                column: "AttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_RaceAttributes_RaceId",
                table: "RaceAttributes",
                column: "RaceId");

            migrationBuilder.CreateIndex(
                name: "IX_RaceSkillBonuses_RaceId",
                table: "RaceSkillBonuses",
                column: "RaceId");

            migrationBuilder.CreateIndex(
                name: "IX_RaceSkillBonuses_SkillId",
                table: "RaceSkillBonuses",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_RaceSpells_RaceId",
                table: "RaceSpells",
                column: "RaceId");

            migrationBuilder.CreateIndex(
                name: "IX_RaceSpells_SpellId",
                table: "RaceSpells",
                column: "SpellId");

            migrationBuilder.CreateIndex(
                name: "IX_RaceStats_RaceId",
                table: "RaceStats",
                column: "RaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RaceAttributes");

            migrationBuilder.DropTable(
                name: "RaceSkillBonuses");

            migrationBuilder.DropTable(
                name: "RaceSpells");

            migrationBuilder.DropTable(
                name: "RaceStats");

            migrationBuilder.DropTable(
                name: "Races");
        }
    }
}
