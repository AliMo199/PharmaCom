using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaCom.DataInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddressUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Addresses",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_ApplicationUserId",
                table: "Addresses",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_AspNetUsers_ApplicationUserId",
                table: "Addresses",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_AspNetUsers_ApplicationUserId",
                table: "Addresses");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_ApplicationUserId",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Addresses");
        }
    }
}
