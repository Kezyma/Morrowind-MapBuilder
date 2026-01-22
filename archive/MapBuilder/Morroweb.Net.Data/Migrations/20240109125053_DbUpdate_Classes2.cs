#nullable disable
using Microsoft.EntityFrameworkCore.Migrations;
namespace Morroweb.Net.Migrations
{
    /// <inheritdoc />
    public partial class DbUpdate_Classes2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpecialisationId",
                table: "Classes",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_SpecialisationId",
                table: "Classes",
                column: "SpecialisationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Specialisations_SpecialisationId",
                table: "Classes",
                column: "SpecialisationId",
                principalTable: "Specialisations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Specialisations_SpecialisationId",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_Classes_SpecialisationId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "SpecialisationId",
                table: "Classes");
        }
    }
}
