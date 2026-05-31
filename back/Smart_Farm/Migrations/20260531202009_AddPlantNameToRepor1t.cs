using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smart_Farm.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantNameToRepor1t : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GeminiArabicReport",
                table: "AI_Diagnosis",
                newName: "GrogArabicReport");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GrogArabicReport",
                table: "AI_Diagnosis",
                newName: "GeminiArabicReport");
        }
    }
}
