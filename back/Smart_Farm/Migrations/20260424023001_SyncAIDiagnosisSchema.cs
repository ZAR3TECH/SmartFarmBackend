using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smart_Farm.Migrations
{
    /// <inheritdoc />
    public partial class SyncAIDiagnosisSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageBytes",
                table: "AI_Diagnosis");

            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "AI_Diagnosis");

            migrationBuilder.DropColumn(
                name: "ImageFileName",
                table: "AI_Diagnosis");

            migrationBuilder.RenameColumn(
                name: "Url",
                table: "PRODUCT_IMAGE",
                newName: "Product_image");

            migrationBuilder.AlterColumn<string>(
                name: "PhotoUrl",
                table: "USERS",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "plant_image",
                table: "AI_Diagnosis",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "plant_image",
                table: "AI_Diagnosis");

            migrationBuilder.RenameColumn(
                name: "Product_image",
                table: "PRODUCT_IMAGE",
                newName: "Url");

            migrationBuilder.AlterColumn<string>(
                name: "PhotoUrl",
                table: "USERS",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageBytes",
                table: "AI_Diagnosis",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "AI_Diagnosis",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageFileName",
                table: "AI_Diagnosis",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);
        }
    }
}
