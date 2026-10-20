using CenterApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CenterApp.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261020180000_BilingualText")]
    public class BilingualText : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            Add(migrationBuilder, "GradeLevels", "NameEn", 250);
            Add(migrationBuilder, "Subjects", "NameEn", 250);
            Add(migrationBuilder, "Teachers", "NameEn", 250);
            Add(migrationBuilder, "Students", "NameEn", 250);
            Add(migrationBuilder, "Students", "ParentNameEn", 250);
            Add(migrationBuilder, "Students", "NotesEn", 250);
            Add(migrationBuilder, "Groups", "NameEn", 250);
            Add(migrationBuilder, "GroupSchedules", "RoomEn", 250);
            Add(migrationBuilder, "Sessions", "NotesEn", 250);
            Add(migrationBuilder, "SessionAttendances", "NoteEn", 250);
            Add(migrationBuilder, "Payments", "NoteEn", 250);
            Add(migrationBuilder, "Materials", "TitleEn", 250);
            Add(migrationBuilder, "Materials", "DescriptionEn", 2000);
            Add(migrationBuilder, "Homeworks", "TitleEn", 250);
            Add(migrationBuilder, "Homeworks", "DescriptionEn", 2000);
            Add(migrationBuilder, "OnlineExams", "TitleEn", 250);
            Add(migrationBuilder, "OnlineExams", "DescriptionEn", 2000);
            Add(migrationBuilder, "OnlineExamQuestions", "TextEn", 2000);
            Add(migrationBuilder, "OnlineExamChoices", "TextEn", 1000);
            Add(migrationBuilder, "OfflineExams", "TitleEn", 250);
            Add(migrationBuilder, "AccessGraces", "ReasonEn", 250);
            Add(migrationBuilder, "AspNetUsers", "FullNameEn", 250);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            Drop(migrationBuilder, "GradeLevels", "NameEn");
            Drop(migrationBuilder, "Subjects", "NameEn");
            Drop(migrationBuilder, "Teachers", "NameEn");
            Drop(migrationBuilder, "Students", "NameEn");
            Drop(migrationBuilder, "Students", "ParentNameEn");
            Drop(migrationBuilder, "Students", "NotesEn");
            Drop(migrationBuilder, "Groups", "NameEn");
            Drop(migrationBuilder, "GroupSchedules", "RoomEn");
            Drop(migrationBuilder, "Sessions", "NotesEn");
            Drop(migrationBuilder, "SessionAttendances", "NoteEn");
            Drop(migrationBuilder, "Payments", "NoteEn");
            Drop(migrationBuilder, "Materials", "TitleEn");
            Drop(migrationBuilder, "Materials", "DescriptionEn");
            Drop(migrationBuilder, "Homeworks", "TitleEn");
            Drop(migrationBuilder, "Homeworks", "DescriptionEn");
            Drop(migrationBuilder, "OnlineExams", "TitleEn");
            Drop(migrationBuilder, "OnlineExams", "DescriptionEn");
            Drop(migrationBuilder, "OnlineExamQuestions", "TextEn");
            Drop(migrationBuilder, "OnlineExamChoices", "TextEn");
            Drop(migrationBuilder, "OfflineExams", "TitleEn");
            Drop(migrationBuilder, "AccessGraces", "ReasonEn");
            Drop(migrationBuilder, "AspNetUsers", "FullNameEn");
        }

        private static void Add(MigrationBuilder migrationBuilder, string table, string column, int length)
            => migrationBuilder.AddColumn<string>(name: column, table: table, type: $"nvarchar({length})", maxLength: length, nullable: true);

        private static void Drop(MigrationBuilder migrationBuilder, string table, string column)
            => migrationBuilder.DropColumn(name: column, table: table);
    }
}
