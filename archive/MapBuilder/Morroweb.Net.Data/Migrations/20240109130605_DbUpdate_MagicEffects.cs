#nullable disable
using Microsoft.EntityFrameworkCore.Migrations;
namespace Morroweb.Net.Migrations
{
    /// <inheritdoc />
    public partial class DbUpdate_MagicEffects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MagicEffects",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SchoolId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BaseCost = table.Column<float>(type: "real", nullable: false),
                    Flags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Speed = table.Column<float>(type: "real", nullable: false),
                    Size = table.Column<float>(type: "real", nullable: false),
                    SizeCap = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MagicEffects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MagicEffects_Skills_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MagicEffects_SchoolId",
                table: "MagicEffects",
                column: "SchoolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MagicEffects");
        }
    }
}
