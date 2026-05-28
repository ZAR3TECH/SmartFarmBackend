using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smart_Farm.Migrations
{
    /// <inheritdoc />
    public partial class FixModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__PRODUCT__Cid__46E78A0C",
                table: "PRODUCT");

            migrationBuilder.DropIndex(
                name: "IX_PRODUCT_Cid",
                table: "PRODUCT");

            migrationBuilder.DropColumn(
                name: "Cid",
                table: "PRODUCT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Cid",
                table: "PRODUCT",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PRODUCT_Cid",
                table: "PRODUCT",
                column: "Cid");

            migrationBuilder.AddForeignKey(
                name: "FK__PRODUCT__Cid__46E78A0C",
                table: "PRODUCT",
                column: "Cid",
                principalTable: "CROP",
                principalColumn: "Cid");
        }
    }
}
