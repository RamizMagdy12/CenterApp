using System;
using CenterApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CenterApp.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261014094300_ExamRetake")]
    public class ExamRetake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RetakeUntil",
                table: "OnlineExamAttempts",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetakeUntil",
                table: "OnlineExamAttempts");
        }
    }
}
