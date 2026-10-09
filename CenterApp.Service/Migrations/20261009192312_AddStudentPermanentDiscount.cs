using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CenterApp.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentPermanentDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscountKind",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountValue",
                table: "Students",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountKind",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "DiscountValue",
                table: "Students");
        }
    }
}
