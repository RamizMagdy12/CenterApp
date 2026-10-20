using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CenterApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GroupCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CodeGroupId",
                table: "Students",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrToken",
                table: "Students",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Groups",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastStudentNumber",
                table: "Groups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "StudentTransfers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    FromGroupId = table.Column<long>(type: "bigint", nullable: false),
                    ToGroupId = table.Column<long>(type: "bigint", nullable: false),
                    OldCode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NewCode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    TransferDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedById = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LastModifiedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LastModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentTransfers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_QrToken",
                table: "Students",
                column: "QrToken",
                unique: true,
                filter: "[QrToken] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Code",
                table: "Groups",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StudentTransfers_StudentId",
                table: "StudentTransfers",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentTransfers");

            migrationBuilder.DropIndex(
                name: "IX_Students_QrToken",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Groups_Code",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "CodeGroupId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "QrToken",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "LastStudentNumber",
                table: "Groups");
        }
    }
}
