using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Semester_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddQRCodeSecretToStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QRCodeSecret",
                table: "Students",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QRCodeSecret",
                table: "Students");
        }
    }
}
