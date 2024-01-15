#nullable disable
using Microsoft.EntityFrameworkCore.Migrations;
namespace Morroweb.Net.Migrations
{
    /// <inheritdoc />
    public partial class DbUpdate_Spells : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Spells",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cost = table.Column<int>(type: "int", nullable: false),
                    Flags = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spells", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MagicEffectInstances",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SpellId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MagicEffectId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SkillId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AttributeId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Range = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Area = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    MinMagnitude = table.Column<int>(type: "int", nullable: false),
                    MaxMagnitude = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MagicEffectInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MagicEffectInstances_Attributes_AttributeId",
                        column: x => x.AttributeId,
                        principalTable: "Attributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MagicEffectInstances_MagicEffects_MagicEffectId",
                        column: x => x.MagicEffectId,
                        principalTable: "MagicEffects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MagicEffectInstances_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MagicEffectInstances_Spells_SpellId",
                        column: x => x.SpellId,
                        principalTable: "Spells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MagicEffectInstances_AttributeId",
                table: "MagicEffectInstances",
                column: "AttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_MagicEffectInstances_MagicEffectId",
                table: "MagicEffectInstances",
                column: "MagicEffectId");

            migrationBuilder.CreateIndex(
                name: "IX_MagicEffectInstances_SkillId",
                table: "MagicEffectInstances",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_MagicEffectInstances_SpellId",
                table: "MagicEffectInstances",
                column: "SpellId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MagicEffectInstances");

            migrationBuilder.DropTable(
                name: "Spells");
        }
    }
}
