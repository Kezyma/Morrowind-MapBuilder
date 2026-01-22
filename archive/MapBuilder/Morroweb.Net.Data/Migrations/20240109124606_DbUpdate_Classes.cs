#nullable disable
using Microsoft.EntityFrameworkCore.Migrations;
namespace Morroweb.Net.Migrations
{
    /// <inheritdoc />
    public partial class DbUpdate_Classes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Classes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Services = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Flags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Attribute1Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Attribute2Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MajorSkill1Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MajorSkill2Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MajorSkill3Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MajorSkill4Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MajorSkill5Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MinorSkill1Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MinorSkill2Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MinorSkill3Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MinorSkill4Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MinorSkill5Id = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Classes_Attributes_Attribute1Id",
                        column: x => x.Attribute1Id,
                        principalTable: "Attributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Classes_Attributes_Attribute2Id",
                        column: x => x.Attribute2Id,
                        principalTable: "Attributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Classes_Skills_MajorSkill1Id",
                        column: x => x.MajorSkill1Id,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Classes_Skills_MajorSkill2Id",
                        column: x => x.MajorSkill2Id,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Classes_Skills_MajorSkill3Id",
                        column: x => x.MajorSkill3Id,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Classes_Skills_MajorSkill4Id",
                        column: x => x.MajorSkill4Id,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Classes_Skills_MajorSkill5Id",
                        column: x => x.MajorSkill5Id,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Classes_Skills_MinorSkill1Id",
                        column: x => x.MinorSkill1Id,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Classes_Skills_MinorSkill2Id",
                        column: x => x.MinorSkill2Id,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Classes_Skills_MinorSkill3Id",
                        column: x => x.MinorSkill3Id,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Classes_Skills_MinorSkill4Id",
                        column: x => x.MinorSkill4Id,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Classes_Skills_MinorSkill5Id",
                        column: x => x.MinorSkill5Id,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Classes_Attribute1Id",
                table: "Classes",
                column: "Attribute1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_Attribute2Id",
                table: "Classes",
                column: "Attribute2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_MajorSkill1Id",
                table: "Classes",
                column: "MajorSkill1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_MajorSkill2Id",
                table: "Classes",
                column: "MajorSkill2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_MajorSkill3Id",
                table: "Classes",
                column: "MajorSkill3Id");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_MajorSkill4Id",
                table: "Classes",
                column: "MajorSkill4Id");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_MajorSkill5Id",
                table: "Classes",
                column: "MajorSkill5Id");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_MinorSkill1Id",
                table: "Classes",
                column: "MinorSkill1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_MinorSkill2Id",
                table: "Classes",
                column: "MinorSkill2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_MinorSkill3Id",
                table: "Classes",
                column: "MinorSkill3Id");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_MinorSkill4Id",
                table: "Classes",
                column: "MinorSkill4Id");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_MinorSkill5Id",
                table: "Classes",
                column: "MinorSkill5Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Classes");
        }
    }
}
