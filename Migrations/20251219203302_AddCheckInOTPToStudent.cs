using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Semester_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckInOTPToStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentCheckInOTP",
                table: "Students",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OTPGeneratedAt",
                table: "Students",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentCheckInOTP",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "OTPGeneratedAt",
                table: "Students");
        }
    }
}
