using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CenterApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Exams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OfflineExams",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GroupId = table.Column<long>(type: "bigint", nullable: false),
                    TeacherId = table.Column<long>(type: "bigint", nullable: true),
                    ExamDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_OfflineExams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfflineExams_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineExams_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnlineExams",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TeacherId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OpensAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosesAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResultsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_OnlineExams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnlineExams_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfflineExamGrades",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OfflineExamId = table.Column<long>(type: "bigint", nullable: false),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
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
                    table.PrimaryKey("PK_OfflineExamGrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfflineExamGrades_OfflineExams_OfflineExamId",
                        column: x => x.OfflineExamId,
                        principalTable: "OfflineExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineExamGrades_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnlineExamAttempts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<long>(type: "bigint", nullable: false),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ViolationCount = table.Column<int>(type: "int", nullable: false),
                    EndReason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
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
                    table.PrimaryKey("PK_OnlineExamAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnlineExamAttempts_OnlineExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "OnlineExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnlineExamAttempts_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnlineExamGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<long>(type: "bigint", nullable: false),
                    GroupId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_OnlineExamGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnlineExamGroups_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnlineExamGroups_OnlineExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "OnlineExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnlineExamQuestions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<long>(type: "bigint", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_OnlineExamQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnlineExamQuestions_OnlineExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "OnlineExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnlineExamChoices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<long>(type: "bigint", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_OnlineExamChoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnlineExamChoices_OnlineExamQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "OnlineExamQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnlineExamAnswers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttemptId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionId = table.Column<long>(type: "bigint", nullable: false),
                    ChoiceId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_OnlineExamAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnlineExamAnswers_OnlineExamAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "OnlineExamAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnlineExamAnswers_OnlineExamChoices_ChoiceId",
                        column: x => x.ChoiceId,
                        principalTable: "OnlineExamChoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnlineExamAnswers_OnlineExamQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "OnlineExamQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineExamGrades_OfflineExamId_StudentId",
                table: "OfflineExamGrades",
                columns: new[] { "OfflineExamId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfflineExamGrades_StudentId",
                table: "OfflineExamGrades",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineExams_GroupId_ExamDate",
                table: "OfflineExams",
                columns: new[] { "GroupId", "ExamDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineExams_TeacherId",
                table: "OfflineExams",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineExamAnswers_AttemptId_QuestionId",
                table: "OnlineExamAnswers",
                columns: new[] { "AttemptId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnlineExamAnswers_ChoiceId",
                table: "OnlineExamAnswers",
                column: "ChoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineExamAnswers_QuestionId",
                table: "OnlineExamAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineExamAttempts_ExamId_StudentId",
                table: "OnlineExamAttempts",
                columns: new[] { "ExamId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnlineExamAttempts_StudentId",
                table: "OnlineExamAttempts",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineExamChoices_QuestionId",
                table: "OnlineExamChoices",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineExamGroups_ExamId_GroupId",
                table: "OnlineExamGroups",
                columns: new[] { "ExamId", "GroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineExamGroups_GroupId",
                table: "OnlineExamGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineExamQuestions_ExamId_SortOrder",
                table: "OnlineExamQuestions",
                columns: new[] { "ExamId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineExams_Status_OpensAt",
                table: "OnlineExams",
                columns: new[] { "Status", "OpensAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineExams_TeacherId",
                table: "OnlineExams",
                column: "TeacherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfflineExamGrades");

            migrationBuilder.DropTable(
                name: "OnlineExamAnswers");

            migrationBuilder.DropTable(
                name: "OnlineExamGroups");

            migrationBuilder.DropTable(
                name: "OfflineExams");

            migrationBuilder.DropTable(
                name: "OnlineExamAttempts");

            migrationBuilder.DropTable(
                name: "OnlineExamChoices");

            migrationBuilder.DropTable(
                name: "OnlineExamQuestions");

            migrationBuilder.DropTable(
                name: "OnlineExams");
        }
    }
}
