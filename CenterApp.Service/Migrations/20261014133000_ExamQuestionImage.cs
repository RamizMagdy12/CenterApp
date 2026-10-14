using CenterApp.Service.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CenterApp.Service.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261014133000_ExamQuestionImage")]
    public class ExamQuestionImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageStoredName",
                table: "OnlineExamQuestions",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageStoredName",
                table: "OnlineExamQuestions");
        }
    }
}
